
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;
using WMS.API.DTOs.PutawayTasks;
using System.Data;

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
                .Include(p => p.ReceiptLine)
                    .ThenInclude(r => r.Item)
                .Include(p => p.Lot)
                .Include(p => p.FromBin)
                .Include(p => p.ToBin)
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
                        ? p.FromBin.BinCode : null,
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

        [HttpGet("debug-mapping")]
        public IActionResult DebugMapping()
        {
            var entityTypes = new[]
            {
        typeof(PutawayTask),
        typeof(GoodsReceiptLine),
        typeof(StorageBin),
        typeof(InventoryBalance)
    };

            var result = entityTypes.Select(type =>
            {
                var entity = _context.Model.FindEntityType(type);

                return new
                {
                    Entity = type.Name,
                    Table = entity?.GetTableName(),
                    Schema = entity?.GetSchema()
                };
            });

            return Ok(result);
        }

        // POST: api/putaway-tasks/1/complete

        [HttpPost("{id:long}/complete")]
        public async Task<IActionResult> Complete(long id)
        {
            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                // 1. Load the putaway task.
                var task = await _context.PutawayTasks
                    .Include(p => p.Lot)
                    .FirstOrDefaultAsync(p => p.PutawayId == id);

                if (task == null)
                    return NotFound("Putaway task not found.");

                if (task.Status != "PENDING")
                    return BadRequest(
                        $"Only PENDING tasks can be completed. Current status: {task.Status}.");

                if (task.Quantity <= 0)
                    return BadRequest("Putaway quantity must be greater than zero.");

                // 2. Load the associated goods receipt line.
                var receiptLine = await _context.GoodsReceiptLines
                    .FirstOrDefaultAsync(r =>
                        r.ReceiptLineId == task.ReceiptLineId);

                if (receiptLine == null)
                    return BadRequest("Goods receipt line does not exist.");

                if (receiptLine.LotId != task.LotId)
                    return BadRequest(
                        "The putaway lot does not match the goods receipt line.");

                // 3. Verify that the goods receipt is posted.
                var goodsReceipt = await _context.GoodsReceipts
                    .FirstOrDefaultAsync(r =>
                        r.ReceiptId == receiptLine.ReceiptId);

                if (goodsReceipt == null ||
                    goodsReceipt.Status != "POSTED")
                {
                    return BadRequest(
                        "The associated goods receipt must be posted.");
                }

                // 4. Resolve the source bin.
                // If FromBinId is null, use the receipt's receiving bin.
                long? effectiveFromBinId =
                    task.FromBinId ?? receiptLine.ReceivingBinId;

                if (!effectiveFromBinId.HasValue)
                {
                    return BadRequest(
                        "A source bin is required to complete this putaway task.");
                }

                long fromBinId = effectiveFromBinId.Value;

                // 5. Validate the source and destination bins.
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
                    return BadRequest(
                        "Source or destination bin does not exist or is inactive.");

                if (fromBin.BinId == toBin.BinId)
                    return BadRequest(
                        "Source and destination bins cannot be the same.");

                if (fromBin.Zone == null || toBin.Zone == null ||
                    fromBin.Zone.WarehouseId != goodsReceipt.WarehouseId ||
                    toBin.Zone.WarehouseId != goodsReceipt.WarehouseId)
                {
                    return BadRequest(
                        "Both bins must belong to the goods receipt warehouse.");
                }

                // 6. Determine the permitted inventory status.
                // Pending quality lots must only move QUARANTINE stock.
                string qualityStatus = task.Lot.QualityStatus.ToUpper();

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
                    return BadRequest(
                        $"Putaway is not permitted for lot quality status: {qualityStatus}.");
                }

                // 7. Load only the appropriate stock-status balance.
                var sourceBalances = await _context.InventoryBalances
                    .Where(b =>
                        b.LotId == task.LotId &&
                        b.BinId == fromBinId &&
                        b.StockStatus == requiredStockStatus)
                    .ToListAsync();

                decimal totalAvailable = sourceBalances.Sum(b =>
                    b.QuantityOnHand - b.QuantityReserved);

                if (totalAvailable < task.Quantity)
                {
                    return BadRequest(
                        $"Insufficient {requiredStockStatus} stock. " +
                        $"Available: {totalAvailable}, Required: {task.Quantity}.");
                }

                // 8. Create the inventory transaction.
                // TRANSFER is an existing allowed transaction type.
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

                // 9. Move inventory while preserving its stock status.
                foreach (var sourceBalance in sourceBalances)
                {
                    if (remainingQuantity <= 0)
                        break;

                    decimal availableQuantity =
                        sourceBalance.QuantityOnHand -
                        sourceBalance.QuantityReserved;

                    if (availableQuantity <= 0)
                        continue;

                    decimal quantityToMove = Math.Min(
                        availableQuantity,
                        remainingQuantity);

                    // Deduct from source.
                    sourceBalance.QuantityOnHand -= quantityToMove;
                    sourceBalance.UpdatedAt = now;

                    // Find or create the destination balance.
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

                    // Record the inventory movement.
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
                    await dbTransaction.RollbackAsync();

                    return BadRequest(
                        "Unable to complete putaway because the source stock was insufficient.");
                }

                // 10. Save the resolved source bin and complete the task.
                task.FromBinId = fromBinId;
                task.Status = "COMPLETED";
                task.CompletedAt = now;

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

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
                    inventoryTransactionId =
                        inventoryTransaction.TransactionId,
                    transactionNumber =
                        inventoryTransaction.TransactionNumber
                });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "An error occurred while completing the putaway.",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }


        // GET: api/putaway-tasks/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var task = await _context.PutawayTasks
                .Include(p => p.ReceiptLine)
                    .ThenInclude(r => r.Item)
                .Include(p => p.Lot)
                .Include(p => p.FromBin)
                .Include(p => p.ToBin)
                .FirstOrDefaultAsync(p => p.PutawayId == id);

            if (task == null)
                return NotFound("Putaway task not found.");

            return Ok(new
            {
                task.PutawayId,
                task.ReceiptLineId,
                task.LotId,
                task.Lot.BatchCode,
                ItemCode = task.ReceiptLine.Item.ItemCode,
                ItemName = task.ReceiptLine.Item.ItemName,
                task.FromBinId,
                FromBinCode = task.FromBin?.BinCode,
                task.ToBinId,
                ToBinCode = task.ToBin.BinCode,
                task.Quantity,
                task.Status,
                task.AssignedTo,
                task.CompletedAt
            });
        }

        [HttpGet("debug-database")]
        public async Task<IActionResult> DebugDatabase()
        {
            var connection = _context.Database.GetDbConnection();

            await _context.Database.OpenConnectionAsync();

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = @"
            SELECT
                current_database(),
                current_schema(),
                current_setting('search_path');
        ";

                await using var reader = await command.ExecuteReaderAsync();
                await reader.ReadAsync();

                return Ok(new
                {
                    Database = reader.GetString(0),
                    Schema = reader.IsDBNull(1) ? null : reader.GetString(1),
                    SearchPath = reader.GetString(2)
                });
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        // POST: api/putaway-tasks
        [HttpPost]
        public async Task<IActionResult> Create(
            CreatePutawayTaskDto dto)
        {
            if (dto.Quantity <= 0)
                return BadRequest(
                    "Quantity must be greater than zero.");

            var receiptLine = await _context.GoodsReceiptLines
                .FirstOrDefaultAsync(r =>
                    r.ReceiptLineId == dto.ReceiptLineId);

            if (receiptLine == null)
                return BadRequest("Goods receipt line does not exist.");

            var goodsReceipt = await _context.GoodsReceipts
                .FirstOrDefaultAsync(r =>
                    r.ReceiptId == receiptLine.ReceiptId);

            if (goodsReceipt == null || goodsReceipt.Status != "POSTED")
            {
                return BadRequest(
                    "Goods receipt must be posted before creating a putaway task.");
            }

            if (receiptLine.LotId != dto.LotId)
                return BadRequest(
                    "The selected lot does not match the receipt line.");

            var toBin = await _context.StorageBins
                .Include(b => b.Zone)
                .FirstOrDefaultAsync(b =>
                    b.BinId == dto.ToBinId && b.IsActive);

            if (toBin == null)
                return BadRequest(
                    "Destination bin does not exist or is inactive.");

            if (toBin.Zone == null)
                return BadRequest(
                    "Destination bin has no assigned warehouse zone.");

            if (toBin.Zone.WarehouseId !=
                goodsReceipt.WarehouseId)
            {
                return BadRequest(
                    "Destination bin must belong to the receipt warehouse.");
            }

            if (dto.FromBinId.HasValue)
            {
                if (dto.FromBinId.Value == dto.ToBinId)
                    return BadRequest(
                        "Source and destination bins cannot be the same.");

                var fromBin = await _context.StorageBins
                    .Include(b => b.Zone)
                    .FirstOrDefaultAsync(b =>
                        b.BinId == dto.FromBinId.Value && b.IsActive);

                if (fromBin == null)
                    return BadRequest(
                        "Source bin does not exist or is inactive.");

                if (fromBin.Zone == null ||
                    fromBin.Zone.WarehouseId != toBin.Zone.WarehouseId)
                {
                    return BadRequest(
                        "Source and destination bins must belong to the same warehouse.");
                }

                // Verify available inventory in the source bin.
                decimal sourceAvailable = await _context.InventoryBalances
                    .Where(b =>
                        b.LotId == dto.LotId &&
                        b.BinId == dto.FromBinId.Value)
                    .SumAsync(b =>
                        (decimal?)(b.QuantityOnHand - b.QuantityReserved))
                    ?? 0;

                // Include existing pending tasks to avoid over-assignment.
                decimal alreadyPending = await _context.PutawayTasks
                    .Where(p =>
                        p.LotId == dto.LotId &&
                        p.FromBinId == dto.FromBinId.Value &&
                        p.Status == "PENDING")
                    .SumAsync(p => (decimal?)p.Quantity) ?? 0;

                decimal remainingSource =
                    sourceAvailable - alreadyPending;

                if (dto.Quantity > remainingSource)
                    return BadRequest(
                        $"Insufficient available stock in source bin. Available: {remainingSource}.");
            }

            decimal alreadyAssigned = await _context.PutawayTasks
                .Where(p =>
                    p.ReceiptLineId == dto.ReceiptLineId &&
                    p.Status != "CANCELLED")
                .SumAsync(p => (decimal?)p.Quantity) ?? 0;

            decimal remainingQuantity =
                receiptLine.ReceivedQuantity - alreadyAssigned;

            if (dto.Quantity > remainingQuantity)
                return BadRequest(
                    $"Putaway quantity exceeds remaining receipt quantity. Remaining: {remainingQuantity}.");

            if (dto.AssignedTo.HasValue)
            {
                bool userExists = await _context.AppUsers
                    .AnyAsync(u =>
                        u.UserId == dto.AssignedTo.Value);

                if (!userExists)
                    return BadRequest(
                        "Assigned user does not exist.");
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

            return CreatedAtAction(
                nameof(GetById),
                new { id = putawayTask.PutawayId },
                new
                {
                    message = "Putaway task created successfully.",
                    putawayId = putawayTask.PutawayId,
                    status = putawayTask.Status
                });
        }
    }
}
