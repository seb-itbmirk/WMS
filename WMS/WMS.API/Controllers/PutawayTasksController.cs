
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.PutawayTasks;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/putaway-tasks")]
    [ApiController]
    public class PutawayTasksController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public PutawayTasksController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/putaway-tasks
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _context.PutawayTasks
                .AsNoTracking()
                .OrderByDescending(p => p.PutawayId)
                .Select(p => new
                {
                    p.PutawayId,
                    p.ReceiptLineId,
                    p.LotId,
                    BatchCode = p.Lot.BatchCode,
                    ItemCode = p.ReceiptLine.Item.ItemCode,
                    ItemName = p.ReceiptLine.Item.ItemName,
                    p.FromBinId,
                    FromBinCode = p.FromBin != null
                        ? p.FromBin.BinCode
                        : null,
                    p.ToBinId,
                    ToBinCode = p.ToBin.BinCode,
                    p.Quantity,
                    p.Status,
                    p.AssignedTo,
                    p.CompletedAt
                })
                .ToListAsync();

            return Ok(tasks);
        }

        // GET: api/putaway-tasks/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var task = await _context.PutawayTasks
                .AsNoTracking()
                .Where(p => p.PutawayId == id)
                .Select(p => new
                {
                    p.PutawayId,
                    p.ReceiptLineId,
                    p.LotId,
                    BatchCode = p.Lot.BatchCode,
                    ItemCode = p.ReceiptLine.Item.ItemCode,
                    ItemName = p.ReceiptLine.Item.ItemName,
                    p.FromBinId,
                    FromBinCode = p.FromBin != null
                        ? p.FromBin.BinCode
                        : null,
                    p.ToBinId,
                    ToBinCode = p.ToBin.BinCode,
                    p.Quantity,
                    p.Status,
                    p.AssignedTo,
                    p.CompletedAt
                })
                .FirstOrDefaultAsync();

            if (task == null)
            {
                return NotFound(new
                {
                    message = "Putaway task not found."
                });
            }

            return Ok(task);
        }

        // POST: api/putaway-tasks
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreatePutawayTaskDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Putaway task details are required."
                });
            }

            if (dto.ReceiptLineId <= 0 ||
                dto.LotId <= 0 ||
                dto.ToBinId <= 0 ||
                dto.Quantity <= 0)
            {
                return BadRequest(new
                {
                    message = "Enter valid receipt line, lot, destination bin, and positive quantity."
                });
            }

            if (dto.FromBinId.HasValue &&
                dto.FromBinId.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "Source bin ID must be valid."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var receiptLine = await _context.GoodsReceiptLines
                    .FirstOrDefaultAsync(r =>
                        r.ReceiptLineId == dto.ReceiptLineId);

                if (receiptLine == null)
                {
                    return NotFound(new
                    {
                        message = "Goods receipt line not found."
                    });
                }

                if (receiptLine.LotId != dto.LotId)
                {
                    return BadRequest(new
                    {
                        message = "The selected lot does not match the goods receipt line."
                    });
                }

                var goodsReceipt = await _context.GoodsReceipts
                    .FirstOrDefaultAsync(r =>
                        r.ReceiptId == receiptLine.ReceiptId);

                if (goodsReceipt == null ||
                    goodsReceipt.Status != "POSTED")
                {
                    return BadRequest(new
                    {
                        message = "Goods receipt must be posted before creating a putaway task."
                    });
                }

                var lot = await _context.InventoryLots
                    .FirstOrDefaultAsync(l =>
                        l.LotId == dto.LotId);

                if (lot == null)
                {
                    return NotFound(new
                    {
                        message = "Inventory lot not found."
                    });
                }

                if (lot.ItemId != receiptLine.ItemId)
                {
                    return BadRequest(new
                    {
                        message = "The inventory lot item does not match the receipt line item."
                    });
                }

                // Resolve the source bin.
                // If no source bin is supplied, use the receiving bin.
                long? effectiveFromBinId =
                    dto.FromBinId ?? receiptLine.ReceivingBinId;

                if (!effectiveFromBinId.HasValue)
                {
                    return BadRequest(new
                    {
                        message = "A source bin is required. Select a source bin or assign a receiving bin to the receipt line."
                    });
                }

                long fromBinId = effectiveFromBinId.Value;

                if (fromBinId == dto.ToBinId)
                {
                    return BadRequest(new
                    {
                        message = "Source and destination bins cannot be the same."
                    });
                }

                var fromBin = await _context.StorageBins
                    .Include(b => b.Zone)
                    .FirstOrDefaultAsync(b =>
                        b.BinId == fromBinId &&
                        b.IsActive);

                if (fromBin == null)
                {
                    return BadRequest(new
                    {
                        message = "Source bin does not exist or is inactive."
                    });
                }

                var toBin = await _context.StorageBins
                    .Include(b => b.Zone)
                    .FirstOrDefaultAsync(b =>
                        b.BinId == dto.ToBinId &&
                        b.IsActive);

                if (toBin == null)
                {
                    return BadRequest(new
                    {
                        message = "Destination bin does not exist or is inactive."
                    });
                }

                if (fromBin.Zone == null || toBin.Zone == null)
                {
                    return BadRequest(new
                    {
                        message = "Source and destination bins must belong to warehouse zones."
                    });
                }

                if (fromBin.Zone.WarehouseId != goodsReceipt.WarehouseId ||
                    toBin.Zone.WarehouseId != goodsReceipt.WarehouseId)
                {
                    return BadRequest(new
                    {
                        message = "Source and destination bins must belong to the goods receipt warehouse."
                    });
                }

                // Determine which inventory status is permitted
                // based on the lot's quality status.
                string qualityStatus =
                    (lot.QualityStatus ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

                string requiredStockStatus;

                if (qualityStatus == "PENDING")
                {
                    requiredStockStatus = "QUARANTINE";
                }
                else if (qualityStatus == "APPROVED")
                {
                    requiredStockStatus = "AVAILABLE";
                }
                else
                {
                    return BadRequest(new
                    {
                        message = $"Putaway is not permitted for lot quality status: {qualityStatus}."
                    });
                }

                // Verify inventory available in the resolved source bin.
                decimal sourceAvailable =
                    await _context.InventoryBalances
                        .Where(b =>
                            b.LotId == dto.LotId &&
                            b.BinId == fromBinId &&
                            b.StockStatus == requiredStockStatus)
                        .SumAsync(b =>
                            (decimal?)(b.QuantityOnHand -
                                      b.QuantityReserved)) ?? 0m;

                // Exclude quantities already allocated to pending
                // putaway tasks from the same source bin.
                decimal alreadyPending =
                    await _context.PutawayTasks
                        .Where(p =>
                            p.LotId == dto.LotId &&
                            p.Status == "PENDING" &&
                            (p.FromBinId == fromBinId ||
                             (!p.FromBinId.HasValue &&
                              p.ReceiptLine.ReceivingBinId == fromBinId)))
                        .SumAsync(p => (decimal?)p.Quantity) ?? 0m;

                decimal remainingSource =
                    sourceAvailable - alreadyPending;

                if (dto.Quantity > remainingSource)
                {
                    return BadRequest(new
                    {
                        message = "Putaway quantity exceeds available stock in the source bin.",
                        sourceBinId = fromBinId,
                        availableQuantity = sourceAvailable,
                        alreadyAssignedQuantity = alreadyPending,
                        remainingQuantity = remainingSource,
                        requestedQuantity = dto.Quantity
                    });
                }

                // Verify the remaining quantity on the receipt line.
                decimal alreadyAssigned =
                    await _context.PutawayTasks
                        .Where(p =>
                            p.ReceiptLineId == dto.ReceiptLineId &&
                            p.Status != "CANCELLED")
                        .SumAsync(p => (decimal?)p.Quantity) ?? 0m;

                decimal remainingReceiptQuantity =
                    receiptLine.ReceivedQuantity - alreadyAssigned;

                if (dto.Quantity > remainingReceiptQuantity)
                {
                    return BadRequest(new
                    {
                        message = "Putaway quantity exceeds the remaining receipt quantity.",
                        receivedQuantity = receiptLine.ReceivedQuantity,
                        alreadyAssignedQuantity = alreadyAssigned,
                        remainingQuantity = remainingReceiptQuantity,
                        requestedQuantity = dto.Quantity
                    });
                }

                if (dto.AssignedTo.HasValue)
                {
                    bool userExists = await _context.AppUsers
                        .AnyAsync(u =>
                            u.UserId == dto.AssignedTo.Value);

                    if (!userExists)
                    {
                        return BadRequest(new
                        {
                            message = "Assigned user does not exist."
                        });
                    }
                }

                var putawayTask = new PutawayTask
                {
                    ReceiptLineId = dto.ReceiptLineId,
                    LotId = dto.LotId,
                    FromBinId = dto.FromBinId,
                    ToBinId = dto.ToBinId,
                    Quantity = dto.Quantity,
                    Status = "PENDING",
                    AssignedTo = dto.AssignedTo
                };

                _context.PutawayTasks.Add(putawayTask);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = putawayTask.PutawayId },
                    new
                    {
                        message = "Putaway task created successfully.",
                        putawayId = putawayTask.PutawayId,
                        receiptLineId = putawayTask.ReceiptLineId,
                        lotId = putawayTask.LotId,
                        fromBinId = fromBinId,
                        toBinId = putawayTask.ToBinId,
                        quantity = putawayTask.Quantity,
                        assignedTo = putawayTask.AssignedTo,
                        status = putawayTask.Status
                    });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // POST: api/putaway-tasks/1/complete
        [HttpPost("{id:long}/complete")]
        public async Task<IActionResult> Complete(long id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var task = await _context.PutawayTasks
                    .Include(p => p.Lot)
                    .FirstOrDefaultAsync(p =>
                        p.PutawayId == id);

                if (task == null)
                {
                    return NotFound(new
                    {
                        message = "Putaway task not found."
                    });
                }

                if (task.Status != "PENDING")
                {
                    return BadRequest(new
                    {
                        message = $"Only PENDING tasks can be completed. Current status: {task.Status}."
                    });
                }

                if (task.Quantity <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Putaway quantity must be greater than zero."
                    });
                }

                var receiptLine = await _context.GoodsReceiptLines
                    .FirstOrDefaultAsync(r =>
                        r.ReceiptLineId == task.ReceiptLineId);

                if (receiptLine == null)
                {
                    return BadRequest(new
                    {
                        message = "Goods receipt line does not exist."
                    });
                }

                if (receiptLine.LotId != task.LotId ||
                    receiptLine.ItemId != task.Lot.ItemId)
                {
                    return BadRequest(new
                    {
                        message = "Putaway lot does not match the goods receipt line."
                    });
                }

                var goodsReceipt = await _context.GoodsReceipts
                    .FirstOrDefaultAsync(r =>
                        r.ReceiptId == receiptLine.ReceiptId);

                if (goodsReceipt == null ||
                    goodsReceipt.Status != "POSTED")
                {
                    return BadRequest(new
                    {
                        message = "The associated goods receipt must be posted."
                    });
                }

                // Resolve the source bin using the same rule as creation.
                long? effectiveFromBinId =
                    task.FromBinId ?? receiptLine.ReceivingBinId;

                if (!effectiveFromBinId.HasValue)
                {
                    return BadRequest(new
                    {
                        message = "A source bin is required to complete this putaway task."
                    });
                }

                long fromBinId = effectiveFromBinId.Value;

                if (fromBinId == task.ToBinId)
                {
                    return BadRequest(new
                    {
                        message = "Source and destination bins cannot be the same."
                    });
                }

                var fromBin = await _context.StorageBins
                    .Include(b => b.Zone)
                    .FirstOrDefaultAsync(b =>
                        b.BinId == fromBinId &&
                        b.IsActive);

                var toBin = await _context.StorageBins
                    .Include(b => b.Zone)
                    .FirstOrDefaultAsync(b =>
                        b.BinId == task.ToBinId &&
                        b.IsActive);

                if (fromBin == null || toBin == null)
                {
                    return BadRequest(new
                    {
                        message = "Source or destination bin does not exist or is inactive."
                    });
                }

                if (fromBin.Zone == null || toBin.Zone == null ||
                    fromBin.Zone.WarehouseId != goodsReceipt.WarehouseId ||
                    toBin.Zone.WarehouseId != goodsReceipt.WarehouseId)
                {
                    return BadRequest(new
                    {
                        message = "Both bins must belong to the goods receipt warehouse."
                    });
                }

                string qualityStatus =
                    (task.Lot.QualityStatus ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

                string requiredStockStatus;

                if (qualityStatus == "PENDING")
                {
                    requiredStockStatus = "QUARANTINE";
                }
                else if (qualityStatus == "APPROVED")
                {
                    requiredStockStatus = "AVAILABLE";
                }
                else
                {
                    return BadRequest(new
                    {
                        message = $"Putaway is not permitted for lot quality status: {qualityStatus}."
                    });
                }

                var sourceBalances = await _context.InventoryBalances
                    .Where(b =>
                        b.LotId == task.LotId &&
                        b.BinId == fromBinId &&
                        b.StockStatus == requiredStockStatus)
                    .OrderBy(b => b.BalanceId)
                    .ToListAsync();

                decimal totalAvailable = sourceBalances.Sum(b =>
                    b.QuantityOnHand - b.QuantityReserved);

                if (totalAvailable < task.Quantity)
                {
                    return BadRequest(new
                    {
                        message = $"Insufficient {requiredStockStatus} stock in the source bin.",
                        availableQuantity = totalAvailable,
                        requestedQuantity = task.Quantity
                    });
                }

                DateTime now = DateTime.UtcNow;

                var inventoryTransaction = new InventoryTransaction
                {
                    TransactionNumber = $"PUT-{Guid.NewGuid():N}",
                    TransactionType = "TRANSFER",
                    TransactionDate = now,
                    ReferenceNumber = $"PUTAWAY-{task.PutawayId}",
                    Remarks = $"Putaway task {task.PutawayId}",
                    Status = "POSTED",
                    PostedAt = now
                };

                _context.InventoryTransactions.Add(inventoryTransaction);

                decimal remainingQuantity = task.Quantity;

                foreach (var sourceBalance in sourceBalances)
                {
                    if (remainingQuantity <= 0)
                    {
                        break;
                    }

                    decimal availableQuantity =
                        sourceBalance.QuantityOnHand -
                        sourceBalance.QuantityReserved;

                    if (availableQuantity <= 0)
                    {
                        continue;
                    }

                    decimal quantityToMove = Math.Min(
                        availableQuantity,
                        remainingQuantity);

                    sourceBalance.QuantityOnHand -= quantityToMove;
                    sourceBalance.UpdatedAt = now;

                    var destinationBalance =
                        await _context.InventoryBalances
                            .FirstOrDefaultAsync(b =>
                                b.LotId == task.LotId &&
                                b.BinId == task.ToBinId &&
                                b.StockStatus == sourceBalance.StockStatus);

                    if (destinationBalance == null)
                    {
                        destinationBalance = new InventoryBalance
                        {
                            LotId = task.LotId,
                            BinId = task.ToBinId,
                            StockStatus = sourceBalance.StockStatus,
                            QuantityOnHand = quantityToMove,
                            QuantityReserved = 0,
                            UpdatedAt = now
                        };

                        _context.InventoryBalances.Add(destinationBalance);
                    }
                    else
                    {
                        destinationBalance.QuantityOnHand += quantityToMove;
                        destinationBalance.UpdatedAt = now;
                    }

                    _context.InventoryTransactionLines.Add(
                        new InventoryTransactionLine
                        {
                            Transaction = inventoryTransaction,
                            LotId = task.LotId,
                            FromBinId = fromBinId,
                            ToBinId = task.ToBinId,
                            Quantity = quantityToMove,
                            UomId = receiptLine.UomId,
                            LineRemarks =
                                $"Putaway task {task.PutawayId}; " +
                                $"Stock status: {sourceBalance.StockStatus}"
                        });

                    remainingQuantity -= quantityToMove;
                }

                if (remainingQuantity > 0)
                {
                    return BadRequest(new
                    {
                        message = "Unable to complete putaway because the source stock was insufficient.",
                        remainingQuantity
                    });
                }

                task.FromBinId = fromBinId;
                task.Status = "COMPLETED";
                task.CompletedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Putaway completed successfully.",
                    putawayId = task.PutawayId,
                    lotId = task.LotId,
                    batchCode = task.Lot.BatchCode,
                    fromBinId = fromBinId,
                    toBinId = task.ToBinId,
                    quantityMoved = task.Quantity,
                    stockStatus = requiredStockStatus,
                    status = task.Status,
                    completedAt = task.CompletedAt,
                    inventoryTransactionId =
                        inventoryTransaction.TransactionId,
                    transactionNumber =
                        inventoryTransaction.TransactionNumber
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "An error occurred while completing the putaway.",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
    }
}
