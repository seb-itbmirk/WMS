
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/production-orders/{productionOrderId:long}")]
    [ApiController]
    public class ProductionMaterialReservationsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public ProductionMaterialReservationsController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/production-orders/1/reservations
        [HttpGet("reservations")]
        public async Task<IActionResult> GetReservations(long productionOrderId)
        {
            var orderExists = await _context.ProductionOrders
                .AnyAsync(p => p.ProductionOrderId == productionOrderId);

            if (!orderExists)
            {
                return NotFound(new
                {
                    message = "Production order not found."
                });
            }

            var reservations = await _context.InventoryReservations
                .Where(r =>
                    r.ReferenceType == "PRODUCTION_ORDER" &&
                    r.ReferenceId == productionOrderId)
                .OrderBy(r => r.ReservationId)
                .Select(r => new
                {
                    r.ReservationId,
                    r.ReferenceType,
                    r.ReferenceId,

                    r.LotId,
                    r.Lot.BatchCode,
                    r.Lot.ItemId,
                    ItemCode = r.Lot.Item.ItemCode,
                    ItemName = r.Lot.Item.ItemName,
                    r.Lot.ExpiryDate,

                    r.BinId,
                    BinCode = r.Bin.BinCode,

                    r.ReservedQuantity,
                    r.Status,
                    r.CreatedAt,
                    r.CreatedBy
                })
                .ToListAsync();

            return Ok(reservations);
        }

        // POST: api/production-orders/1/reserve
        [HttpPost("reserve")]
        public async Task<IActionResult> ReserveMaterials(long productionOrderId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var order = await _context.ProductionOrders
                    .Include(p => p.Warehouse)
                    .Include(p => p.ProductionMaterialRequirements)
                        .ThenInclude(r => r.Item)
                    .FirstOrDefaultAsync(p =>
                        p.ProductionOrderId == productionOrderId);

                if (order == null)
                {
                    await transaction.RollbackAsync();

                    return NotFound(new
                    {
                        message = "Production order not found."
                    });
                }

                if (!order.Warehouse.IsActive)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "Production warehouse is inactive."
                    });
                }

                if (order.ProductionStatus != "DRAFT" &&
                    order.ProductionStatus != "PLANNED" &&
                    order.ProductionStatus != "RELEASED")
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "Materials cannot be reserved for this production order status."
                    });
                }

                if (!order.ProductionMaterialRequirements.Any())
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "Production order has no material requirements."
                    });
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var createdReservations = new List<object>();

                foreach (var requirement in order.ProductionMaterialRequirements)
                {
                    if (!requirement.Item.IsActive)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = $"Item {requirement.Item.ItemCode} is inactive."
                        });
                    }

                    var remainingQuantity =
                        requirement.RequiredQuantity -
                        requirement.ReservedQuantity;

                    if (remainingQuantity <= 0)
                    {
                        continue;
                    }

                    // Find eligible inventory balances in the production warehouse.
                    var balances = await _context.InventoryBalances
                        .Include(b => b.Lot)
                            .ThenInclude(l => l.Item)
                        .Include(b => b.Bin)
                            .ThenInclude(bin => bin.Zone)
                        .Where(b =>
                            b.Lot.ItemId == requirement.ItemId &&
                            b.Bin.Zone.WarehouseId == order.WarehouseId &&
                            b.Bin.IsActive &&
                            b.StockStatus == "AVAILABLE" &&
                            b.Lot.QualityStatus == "APPROVED" &&
                            b.QuantityOnHand > b.QuantityReserved &&
                            (
                                !b.Lot.ExpiryDate.HasValue ||
                                b.Lot.ExpiryDate.Value >= today
                            ) &&
                            (
                                !requirement.Item.RequiresExpiry ||
                                b.Lot.ExpiryDate.HasValue
                            ))
                        .ToListAsync();

                    // Apply FIFO or FEFO.
                    if (requirement.Item.StockRotationMethod == "FIFO")
                    {
                        balances = balances
                            .OrderBy(b => b.Lot.ReceivedDate)
                            .ThenBy(b => b.Lot.LotId)
                            .ThenBy(b => b.BinId)
                            .ToList();
                    }
                    else if (requirement.Item.StockRotationMethod == "FEFO")
                    {
                        balances = balances
                            .OrderBy(b => b.Lot.ExpiryDate.HasValue ? 0 : 1)
                            .ThenBy(b => b.Lot.ExpiryDate)
                            .ThenBy(b => b.Lot.ReceivedDate)
                            .ThenBy(b => b.Lot.LotId)
                            .ThenBy(b => b.BinId)
                            .ToList();
                    }
                    else
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = $"Unsupported stock rotation method for {requirement.Item.ItemCode}."
                        });
                    }

                    var availableTotal = balances.Sum(b =>
                        b.QuantityOnHand - b.QuantityReserved);

                    if (availableTotal < remainingQuantity)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = "Insufficient available stock.",
                            itemCode = requirement.Item.ItemCode,
                            itemName = requirement.Item.ItemName,
                            requiredQuantity = requirement.RequiredQuantity,
                            alreadyReserved = requirement.ReservedQuantity,
                            remainingQuantity,
                            availableQuantity = availableTotal,
                            uomId = requirement.UomId
                        });
                    }

                    var quantityToReserve = remainingQuantity;

                    foreach (var balance in balances)
                    {
                        if (quantityToReserve <= 0)
                        {
                            break;
                        }

                        var availableQuantity =
                            balance.QuantityOnHand -
                            balance.QuantityReserved;

                        if (availableQuantity <= 0)
                        {
                            continue;
                        }

                        var reserveQuantity = Math.Min(
                            availableQuantity,
                            quantityToReserve);

                        // Update inventory balance.
                        balance.QuantityReserved += reserveQuantity;
                        balance.UpdatedAt = DateTime.UtcNow;

                        // Create reservation record.
                        var reservation = new InventoryReservation
                        {
                            LotId = balance.LotId,
                            BinId = balance.BinId,
                            ReferenceType = "PRODUCTION_ORDER",
                            ReferenceId = order.ProductionOrderId,
                            ReservedQuantity = reserveQuantity,
                            Status = "ACTIVE",
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = null
                        };

                        _context.InventoryReservations.Add(reservation);

                        createdReservations.Add(new
                        {
                            balance.LotId,
                            balance.Lot.BatchCode,
                            ItemCode = requirement.Item.ItemCode,
                            balance.BinId,
                            BinCode = balance.Bin.BinCode,
                            ReservedQuantity = reserveQuantity
                        });

                        requirement.ReservedQuantity += reserveQuantity;

                        quantityToReserve -= reserveQuantity;
                    }

                    if (quantityToReserve > 0)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = $"Could not reserve the full quantity for {requirement.Item.ItemCode}."
                        });
                    }
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Production materials reserved successfully.",
                    productionOrderId = order.ProductionOrderId,
                    productionNumber = order.ProductionNumber,
                    warehouseId = order.WarehouseId,
                    reservationCount = createdReservations.Count,
                    reservations = createdReservations
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // POST: api/production-orders/1/release-reservations
        [HttpPost("release-reservations")]
        public async Task<IActionResult> ReleaseReservations(long productionOrderId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var order = await _context.ProductionOrders
                    .Include(p => p.ProductionMaterialRequirements)
                    .FirstOrDefaultAsync(p =>
                        p.ProductionOrderId == productionOrderId);

                if (order == null)
                {
                    await transaction.RollbackAsync();

                    return NotFound(new
                    {
                        message = "Production order not found."
                    });
                }

                // Include Lot because the requirement recalculation below
                // uses reservation.Lot.ItemId.
                var activeReservations = await _context.InventoryReservations
                    .Include(r => r.Lot)
                    .Where(r =>
                        r.ReferenceType == "PRODUCTION_ORDER" &&
                        r.ReferenceId == productionOrderId &&
                        r.Status == "ACTIVE")
                    .ToListAsync();

                if (!activeReservations.Any())
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "No active reservations were found."
                    });
                }

                foreach (var reservation in activeReservations)
                {
                    var balance = await _context.InventoryBalances
                        .FirstOrDefaultAsync(b =>
                            b.LotId == reservation.LotId &&
                            b.BinId == reservation.BinId &&
                            b.StockStatus == "AVAILABLE");

                    if (balance == null)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = $"Inventory balance not found for lot {reservation.LotId} and bin {reservation.BinId}."
                        });
                    }

                    if (balance.QuantityReserved < reservation.ReservedQuantity)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = $"Reserved balance is insufficient for lot {reservation.LotId}."
                        });
                    }

                    // Return reserved quantity to available stock.
                    balance.QuantityReserved -= reservation.ReservedQuantity;
                    balance.UpdatedAt = DateTime.UtcNow;

                    // Mark reservation as released.
                    reservation.Status = "RELEASED";
                }

                // Recalculate reserved quantities for each material requirement.
                foreach (var requirement in order.ProductionMaterialRequirements)
                {
                    var releasedQuantity = activeReservations
                        .Where(r => r.Lot.ItemId == requirement.ItemId)
                        .Sum(r => r.ReservedQuantity);

                    requirement.ReservedQuantity -= releasedQuantity;

                    if (requirement.ReservedQuantity < 0)
                    {
                        requirement.ReservedQuantity = 0;
                    }
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Production material reservations released successfully.",
                    productionOrderId,
                    releasedReservationCount = activeReservations.Count
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
