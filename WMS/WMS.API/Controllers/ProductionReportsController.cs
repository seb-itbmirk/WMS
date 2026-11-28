
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.ProductionReports;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/production-reports")]
    [ApiController]
    public class ProductionReportsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public ProductionReportsController(WmsDbContext context)
        {
            _context = context;
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            return long.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }

        // GET: api/production-reports
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reports = await _context.ProductionOutputs
                .AsNoTracking()
                .OrderByDescending(o => o.ProductionDate)
                .Select(o => new
                {
                    o.OutputId,
                    o.ProductionOrderId,
                    ProductionNumber = o.ProductionOrder.ProductionNumber,

                    FinishedItemCode =
                        o.ProductionOrder.FinishedItem.ItemCode,

                    FinishedItemName =
                        o.ProductionOrder.FinishedItem.ItemName,

                    o.FinishedLotId,
                    BatchCode = o.FinishedLot.BatchCode,

                    o.ProducedQuantity,
                    o.RejectedQuantity,

                    TotalReportedQuantity =
                        o.ProducedQuantity + o.RejectedQuantity,

                    o.UomId,
                    UomCode = o.Uom.UomCode,

                    o.ProductionDate,
                    o.QualityStatus,
                    o.IsPosted,
                    o.Remarks
                })
                .ToListAsync();

            return Ok(reports);
        }

        // GET: api/production-reports/5
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var report = await _context.ProductionOutputs
                .AsNoTracking()
                .Where(o => o.OutputId == id)
                .Select(o => new
                {
                    o.OutputId,
                    o.ProductionOrderId,
                    ProductionNumber = o.ProductionOrder.ProductionNumber,

                    o.FinishedLotId,
                    BatchCode = o.FinishedLot.BatchCode,

                    FinishedItemId = o.ProductionOrder.FinishedItemId,
                    FinishedItemCode =
                        o.ProductionOrder.FinishedItem.ItemCode,

                    FinishedItemName =
                        o.ProductionOrder.FinishedItem.ItemName,

                    o.ProducedQuantity,
                    o.RejectedQuantity,

                    TotalReportedQuantity =
                        o.ProducedQuantity + o.RejectedQuantity,

                    o.UomId,
                    UomCode = o.Uom.UomCode,

                    o.ProductionDate,
                    o.QualityStatus,
                    o.IsPosted,
                    o.Remarks
                })
                .FirstOrDefaultAsync();

            if (report == null)
            {
                return NotFound(new
                {
                    message = "Production report not found."
                });
            }

            return Ok(report);
        }

        // GET: api/production-reports/order/5
        [HttpGet("order/{productionOrderId:long}")]
        public async Task<IActionResult> GetByProductionOrder(
            long productionOrderId)
        {
            var orderExists = await _context.ProductionOrders
                .AnyAsync(p =>
                    p.ProductionOrderId == productionOrderId);

            if (!orderExists)
            {
                return NotFound(new
                {
                    message = "Production order not found."
                });
            }

            var reports = await _context.ProductionOutputs
                .AsNoTracking()
                .Where(o =>
                    o.ProductionOrderId == productionOrderId)
                .OrderByDescending(o => o.ProductionDate)
                .Select(o => new
                {
                    o.OutputId,
                    o.ProductionOrderId,
                    o.FinishedLotId,
                    BatchCode = o.FinishedLot.BatchCode,
                    o.ProducedQuantity,
                    o.RejectedQuantity,
                    TotalReportedQuantity =
                        o.ProducedQuantity + o.RejectedQuantity,
                    UomCode = o.Uom.UomCode,
                    o.ProductionDate,
                    o.QualityStatus,
                    o.IsPosted,
                    o.Remarks
                })
                .ToListAsync();

            return Ok(reports);
        }

        // POST: api/production-reports
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductionReportDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Production report data is required."
                });
            }

            if (dto.ProductionOrderId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid production order is required."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.BatchCode))
            {
                return BadRequest(new
                {
                    message = "Batch code is required."
                });
            }

            if (dto.ProducedQuantity <= 0)
            {
                return BadRequest(new
                {
                    message = "Good produced quantity must be greater than zero."
                });
            }

            if (dto.RejectedQuantity < 0)
            {
                return BadRequest(new
                {
                    message = "Rejected quantity cannot be negative."
                });
            }

            if (dto.ProductionDate == default)
            {
                return BadRequest(new
                {
                    message = "Production date is required."
                });
            }

            var order = await _context.ProductionOrders
                .Include(p => p.FinishedItem)
                .FirstOrDefaultAsync(p =>
                    p.ProductionOrderId == dto.ProductionOrderId);

            if (order == null)
            {
                return NotFound(new
                {
                    message = "Production order not found."
                });
            }

            if (order.ProductionStatus != "RELEASED" &&
                order.ProductionStatus != "IN_PROGRESS")
            {
                return BadRequest(new
                {
                    message = "Only released or in-progress production orders can receive production reports."
                });
            }

            var batchCode = dto.BatchCode.Trim();

            var duplicateBatch = await _context.InventoryLots
                .AnyAsync(l =>
                    l.ItemId == order.FinishedItemId &&
                    l.BatchCode == batchCode);

            if (duplicateBatch)
            {
                return Conflict(new
                {
                    message = "This batch code already exists for the finished item."
                });
            }

            // Planned quantity represents accepted/good output.
            var existingGoodOutput = await _context.ProductionOutputs
                .Where(o =>
                    o.ProductionOrderId == order.ProductionOrderId)
                .SumAsync(o => (decimal?)o.ProducedQuantity) ?? 0;

            if (existingGoodOutput + dto.ProducedQuantity >
                order.PlannedQuantity)
            {
                return BadRequest(new
                {
                    message = "Reported good quantity exceeds the remaining planned production quantity.",
                    PlannedQuantity = order.PlannedQuantity,
                    AlreadyProduced = existingGoodOutput,
                    RemainingQuantity = Math.Max(
                        0,
                        order.PlannedQuantity - existingGoodOutput)
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var productionDate = dto.ProductionDate;

                var lot = new InventoryLot
                {
                    ItemId = order.FinishedItemId,
                    BatchCode = batchCode,
                    ManufactureDate =
                        DateOnly.FromDateTime(productionDate),
                    ReceivedDate =
                        DateOnly.FromDateTime(productionDate),
                    QualityStatus = "PENDING",
                    Notes = dto.Remarks?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.InventoryLots.Add(lot);

                await _context.SaveChangesAsync();

                var output = new ProductionOutput
                {
                    ProductionOrderId = order.ProductionOrderId,
                    FinishedLotId = lot.LotId,
                    ProducedQuantity = dto.ProducedQuantity,
                    RejectedQuantity = dto.RejectedQuantity,
                    UomId = order.UomId,
                    ProductionDate = productionDate,
                    QualityStatus = "PENDING",
                    IsPosted = false,
                    Remarks = dto.Remarks?.Trim()
                };

                _context.ProductionOutputs.Add(output);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = output.OutputId },
                    new
                    {
                        message = "Production report created successfully. It is pending inventory posting.",
                        output.OutputId,
                        output.ProductionOrderId,
                        output.FinishedLotId,
                        BatchCode = lot.BatchCode,
                        output.ProducedQuantity,
                        output.RejectedQuantity,
                        TotalReportedQuantity =
                            output.ProducedQuantity +
                            output.RejectedQuantity,
                        output.QualityStatus,
                        output.IsPosted
                    });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // POST: api/production-reports/5/post
        [HttpPost("{id:long}/post")]
        public async Task<IActionResult> Post(
            long id,
            [FromBody] PostProductionReportDto dto)
        {
            if (dto == null || dto.BinId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid destination bin is required."
                });
            }

            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new
                {
                    message = "Authenticated user information is unavailable."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                var output = await _context.ProductionOutputs
                    .Include(o => o.ProductionOrder)
                        .ThenInclude(p => p.FinishedItem)
                    .Include(o => o.FinishedLot)
                    .FirstOrDefaultAsync(o => o.OutputId == id);

                if (output == null)
                {
                    return NotFound(new
                    {
                        message = "Production report not found."
                    });
                }

                if (output.IsPosted)
                {
                    return Conflict(new
                    {
                        message = "This production report has already been posted."
                    });
                }

                if (output.ProducedQuantity <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Production report has no accepted output to post."
                    });
                }

                var order = output.ProductionOrder;

                if (order.ProductionStatus != "RELEASED" &&
                    order.ProductionStatus != "IN_PROGRESS")
                {
                    return BadRequest(new
                    {
                        message = "The production order is not available for posting."
                    });
                }

                var bin = await _context.StorageBins
                    .Include(b => b.Zone)
                    .FirstOrDefaultAsync(b =>
                        b.BinId == dto.BinId &&
                        b.IsActive);

                if (bin == null)
                {
                    return BadRequest(new
                    {
                        message = "Destination bin does not exist or is inactive."
                    });
                }

                if (bin.Zone.WarehouseId != order.WarehouseId)
                {
                    return BadRequest(new
                    {
                        message = "Destination bin must belong to the production order warehouse."
                    });
                }

                // Create inventory transaction.
                var transactionNumber =
                    $"PROD-{Guid.NewGuid():N}";

                var inventoryTransaction =
                    new InventoryTransaction
                    {
                        TransactionNumber = transactionNumber,
                        TransactionType = "PRODUCTION_OUTPUT",
                        TransactionDate = DateTime.UtcNow,
                        ReferenceNumber = order.ProductionNumber,
                        Remarks =
                            $"Production output posting: {order.ProductionNumber}, batch {output.FinishedLot.BatchCode}",
                        Status = "POSTED",
                        CreatedBy = currentUserId,
                        PostedBy = currentUserId,
                        PostedAt = DateTime.UtcNow
                    };

                _context.InventoryTransactions.Add(
                    inventoryTransaction);

                await _context.SaveChangesAsync();

                // Increase available inventory using accepted output only.
                var balance = await _context.InventoryBalances
                    .FirstOrDefaultAsync(b =>
                        b.LotId == output.FinishedLotId &&
                        b.BinId == bin.BinId &&
                        b.StockStatus == "AVAILABLE");

                if (balance == null)
                {
                    balance = new InventoryBalance
                    {
                        LotId = output.FinishedLotId,
                        BinId = bin.BinId,
                        StockStatus = "AVAILABLE",
                        QuantityOnHand = output.ProducedQuantity,
                        QuantityReserved = 0,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.InventoryBalances.Add(balance);
                }
                else
                {
                    balance.QuantityOnHand +=
                        output.ProducedQuantity;

                    balance.UpdatedAt = DateTime.UtcNow;
                }

                var transactionLine =
                    new InventoryTransactionLine
                    {
                        TransactionId =
                            inventoryTransaction.TransactionId,

                        LotId = output.FinishedLotId,
                        FromBinId = null,
                        ToBinId = bin.BinId,
                        Quantity = output.ProducedQuantity,
                        UomId = output.UomId,
                        LineRemarks =
                            $"Accepted production output. Rejected quantity: {output.RejectedQuantity}"
                    };

                _context.InventoryTransactionLines.Add(
                    transactionLine);

                // Update finished lot and report.
                output.FinishedLot.QualityStatus = "APPROVED";
                output.QualityStatus = "APPROVED";
                output.IsPosted = true;

                // Update order production dates and status.
                var totalGoodOutput = await _context.ProductionOutputs
                    .Where(o =>
                        o.ProductionOrderId == order.ProductionOrderId &&
                        o.IsPosted)
                    .SumAsync(o => (decimal?)o.ProducedQuantity) ?? 0;

                totalGoodOutput += output.ProducedQuantity;

                if (!order.ActualStartDate.HasValue)
                {
                    order.ActualStartDate = output.ProductionDate;
                }

                if (totalGoodOutput >= order.PlannedQuantity)
                {
                    order.ProductionStatus = "COMPLETED";
                    order.ActualEndDate = DateTime.UtcNow;
                }
                else
                {
                    order.ProductionStatus = "IN_PROGRESS";
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Production report posted successfully. Accepted output has been added to available inventory.",

                    output.OutputId,
                    output.ProductionOrderId,
                    order.ProductionNumber,

                    output.ProducedQuantity,
                    output.RejectedQuantity,

                    InventoryAdded = output.ProducedQuantity,

                    inventoryTransaction.TransactionId,
                    inventoryTransaction.TransactionNumber,

                    DestinationBin = bin.BinCode,
                    WarehouseId = bin.Zone.WarehouseId,

                    ProductionStatus = order.ProductionStatus,
                    TotalGoodOutput = totalGoodOutput,

                    RemainingQuantity = Math.Max(
                        0,
                        order.PlannedQuantity - totalGoodOutput),

                    CompletionPercentage = Math.Round(
                        Math.Min(
                            100,
                            totalGoodOutput /
                            order.PlannedQuantity * 100),
                        2)
                });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    message = "The production report could not be posted because of a database conflict. Please refresh and try again."
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
