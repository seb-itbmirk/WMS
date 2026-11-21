
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;

namespace WMS.API.Controllers
{
    [Route("api/production-orders")]
    [ApiController]
    public class ProductionOrderCompletionController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public ProductionOrderCompletionController(WmsDbContext context)
        {
            _context = context;
        }

        // POST: api/production-orders/1/complete
        [HttpPost("{productionOrderId:long}/complete")]
        public async Task<IActionResult> Complete(long productionOrderId)
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

                if (order.ProductionStatus != "IN_PROGRESS")
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "Only IN_PROGRESS production orders can be completed.",
                        currentStatus = order.ProductionStatus
                    });
                }

                // Check whether production output exists.
                var outputs = await _context.ProductionOutputs
                    .Where(o => o.ProductionOrderId == productionOrderId)
                    .ToListAsync();

                if (outputs.Any(o => o.ProducedQuantity <= 0))
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "Production output quantities must be greater than zero."
                    });
                }

                var totalProduced = outputs.Sum(o => o.ProducedQuantity);

                if (totalProduced <= 0)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "Production output must be recorded before completing the order."
                    });
                }

                // Check every material requirement.
                var materialIssues = new List<object>();

                foreach (var requirement in order.ProductionMaterialRequirements)
                {
                    var consumed = await _context.ProductionConsumptions
                        .Where(c =>
                            c.ProductionOrderId == productionOrderId &&
                            c.Lot.ItemId == requirement.ItemId)
                        .SumAsync(c => (decimal?)c.ConsumedQuantity) ?? 0;

                    var wasted = await _context.ProductionWastes
                        .Where(w =>
                            w.ProductionOrderId == productionOrderId &&
                            w.Lot.ItemId == requirement.ItemId)
                        .SumAsync(w => (decimal?)w.WastedQuantity) ?? 0;

                    if (consumed < 0 || wasted < 0)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = "Consumption and waste quantities cannot be negative.",
                            itemId = requirement.ItemId,
                            consumedQuantity = consumed,
                            wastedQuantity = wasted
                        });
                    }

                    var accountedQuantity = consumed + wasted;
                    var remainingQuantity =
                        requirement.IssuedQuantity - accountedQuantity;

                    if (remainingQuantity < 0)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = "Recorded consumption and waste exceed the issued material quantity.",
                            itemId = requirement.ItemId,
                            issuedQuantity = requirement.IssuedQuantity,
                            consumedQuantity = consumed,
                            wastedQuantity = wasted
                        });
                    }

                    if (remainingQuantity > 0)
                    {
                        materialIssues.Add(new
                        {
                            itemId = requirement.ItemId,
                            issuedQuantity = requirement.IssuedQuantity,
                            consumedQuantity = consumed,
                            wastedQuantity = wasted,
                            remainingQuantity
                        });
                    }
                }

                if (materialIssues.Count > 0)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "Production cannot be completed because some issued materials have not been fully accounted for. Record their consumption or waste first.",
                        unaccountedMaterials = materialIssues
                    });
                }

                // Release any remaining active reservations.
                var activeReservations = await _context.InventoryReservations
                    .Where(r =>
                        r.ReferenceType == "PRODUCTION_ORDER" &&
                        r.ReferenceId == productionOrderId &&
                        r.Status == "ACTIVE")
                    .ToListAsync();

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
                            message = "Inventory balance not found for an active production reservation.",
                            reservationId = reservation.ReservationId,
                            lotId = reservation.LotId,
                            binId = reservation.BinId
                        });
                    }

                    if (reservation.ReservedQuantity < 0 ||
                        balance.QuantityReserved < reservation.ReservedQuantity)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = "Inventory reserved quantity is inconsistent with the active reservation.",
                            reservationId = reservation.ReservationId,
                            lotId = reservation.LotId,
                            binId = reservation.BinId,
                            reservationQuantity = reservation.ReservedQuantity,
                            inventoryReservedQuantity = balance.QuantityReserved
                        });
                    }

                    balance.QuantityReserved -= reservation.ReservedQuantity;
                    balance.UpdatedAt = DateTime.UtcNow;

                    reservation.Status = "RELEASED";
                }

                // Update the reserved quantity on each material requirement.
                foreach (var requirement in order.ProductionMaterialRequirements)
                {
                    var releasedQuantity = activeReservations
                        .Where(r => r.LotId > 0)
                        .Where(r => r.Lot.ItemId == requirement.ItemId)
                        .Sum(r => r.ReservedQuantity);

                    if (releasedQuantity > requirement.ReservedQuantity)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = "Released reservation quantity exceeds the reserved quantity recorded on the material requirement.",
                            itemId = requirement.ItemId,
                            requirementReservedQuantity = requirement.ReservedQuantity,
                            releasedQuantity
                        });
                    }

                    requirement.ReservedQuantity -= releasedQuantity;
                }

                // Complete production order.
                order.ProductionStatus = "COMPLETED";
                order.ActualEndDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Production order completed successfully.",
                    productionOrderId = order.ProductionOrderId,
                    productionNumber = order.ProductionNumber,
                    productionStatus = order.ProductionStatus,
                    plannedQuantity = order.PlannedQuantity,
                    totalProduced,
                    releasedReservationCount = activeReservations.Count,
                    actualStartDate = order.ActualStartDate,
                    actualEndDate = order.ActualEndDate
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
