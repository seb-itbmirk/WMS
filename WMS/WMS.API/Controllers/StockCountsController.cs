
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;
using WMS.API.DTOs.StockCounts;

namespace WMS.API.Controllers
{
    [Route("api/stock-counts")]
    [ApiController]
    public class StockCountsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public StockCountsController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/stock-counts
        [HttpGet]
        public async Task<IActionResult> GetStockCounts()
        {
            var counts = await _context.StockCounts
                .Include(sc => sc.Warehouse)
                .OrderByDescending(sc => sc.CountDate)
                .Select(sc => new
                {
                    sc.StockCountId,
                    sc.CountNumber,
                    sc.WarehouseId,
                    WarehouseName = sc.Warehouse.WarehouseName,
                    sc.CountDate,
                    sc.Status,
                    sc.CreatedBy
                })
                .ToListAsync();

            return Ok(counts);
        }

        // GET: api/stock-counts/{id}
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetStockCount(long id)
        {
            var count = await _context.StockCounts
                .Include(sc => sc.Warehouse)
                .Include(sc => sc.StockCountLines)
                    .ThenInclude(line => line.Lot)
                        .ThenInclude(lot => lot.Item)
                .Include(sc => sc.StockCountLines)
                    .ThenInclude(line => line.Bin)
                .Include(sc => sc.StockCountLines)
                    .ThenInclude(line => line.CountedByNavigation)
                .FirstOrDefaultAsync(sc => sc.StockCountId == id);

            if (count == null)
                return NotFound("Stock count not found.");

            var result = new
            {
                count.StockCountId,
                count.CountNumber,
                count.WarehouseId,
                WarehouseName = count.Warehouse.WarehouseName,
                count.CountDate,
                count.Status,
                count.CreatedBy,

                Lines = count.StockCountLines.Select(line => new
                {
                    line.StockCountLineId,
                    line.LotId,
                    line.Lot.BatchCode,
                    line.Lot.Item.ItemCode,
                    line.Lot.Item.ItemName,
                    line.BinId,
                    line.Bin.BinCode,
                    line.SystemQuantity,
                    line.CountedQuantity,
                    line.CountedBy,
                    CountedByName = line.CountedByNavigation == null
                        ? null
                        : line.CountedByNavigation.Username,
                    line.CountedAt
                })
            };

            return Ok(result);
        }

        // POST: api/stock-counts
        [HttpPost]
        public async Task<IActionResult> CreateStockCount(
            [FromBody] CreateStockCountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CountNumber))
                return BadRequest("Count number is required.");

            if (dto.WarehouseId <= 0)
                return BadRequest("Valid warehouse is required.");

            string countNumber = dto.CountNumber.Trim();

            if (await _context.StockCounts.AnyAsync(
                sc => sc.CountNumber == countNumber))
            {
                return Conflict("Count number already exists.");
            }

            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w =>
                    w.WarehouseId == dto.WarehouseId &&
                    w.IsActive);

            if (warehouse == null)
                return BadRequest("Active warehouse not found.");

            if (dto.CreatedBy.HasValue)
            {
                var userExists = await _context.AppUsers
                    .AnyAsync(u => u.UserId == dto.CreatedBy.Value);

                if (!userExists)
                    return BadRequest("Created by user does not exist.");
            }

            if (dto.CountDate == default)
                return BadRequest("Valid count date is required.");

            var count = new StockCount
            {
                CountNumber = countNumber,
                WarehouseId = dto.WarehouseId,
                CountDate = DateTime.SpecifyKind(
                    dto.CountDate, DateTimeKind.Utc),
                Status = "OPEN",
                CreatedBy = dto.CreatedBy
            };

            try
            {
                _context.StockCounts.Add(count);
                await _context.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetStockCount),
                    new { id = count.StockCountId },
                    new
                    {
                        count.StockCountId,
                        count.CountNumber,
                        count.WarehouseId,
                        count.CountDate,
                        count.Status
                    });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new
                {
                    Message = "Database error while creating stock count.",
                    Error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        // POST: api/stock-counts/{id}/lines
        [HttpPost("{id:long}/lines")]
        public async Task<IActionResult> AddStockCountLine(
            long id,
            [FromBody] AddStockCountLineDto dto)
        {
            var count = await _context.StockCounts
                .Include(sc => sc.StockCountLines)
                .FirstOrDefaultAsync(sc => sc.StockCountId == id);

            if (count == null)
                return NotFound("Stock count not found.");

            if (count.Status != "OPEN" &&
                count.Status != "IN_PROGRESS")
            {
                return BadRequest(
                    "Lines can only be added to OPEN or IN_PROGRESS stock counts.");
            }

            if (dto.LotId <= 0 || dto.BinId <= 0)
                return BadRequest("Valid lot and bin are required.");

            var bin = await _context.StorageBins
                .Include(b => b.Zone)
                .FirstOrDefaultAsync(b =>
                    b.BinId == dto.BinId &&
                    b.IsActive &&
                    b.Zone.WarehouseId == count.WarehouseId);

            if (bin == null)
                return BadRequest(
                    "Active bin does not belong to the selected warehouse.");

            var lot = await _context.InventoryLots
                .Include(l => l.Item)
                .FirstOrDefaultAsync(l => l.LotId == dto.LotId);

            if (lot == null)
                return BadRequest("Inventory lot not found.");

            if (count.StockCountLines.Any(line =>
                line.LotId == dto.LotId &&
                line.BinId == dto.BinId))
            {
                return Conflict(
                    "This lot and bin already exist in the stock count.");
            }

            var balance = await _context.InventoryBalances
                .FirstOrDefaultAsync(b =>
                    b.LotId == dto.LotId &&
                    b.BinId == dto.BinId &&
                    b.StockStatus == "AVAILABLE");

            decimal systemQuantity =
                balance?.QuantityOnHand ?? 0;

            var line = new StockCountLine
            {
                StockCountId = id,
                LotId = dto.LotId,
                BinId = dto.BinId,
                SystemQuantity = systemQuantity
            };

            try
            {
                _context.StockCountLines.Add(line);

                if (count.Status == "OPEN")
                    count.Status = "IN_PROGRESS";

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    line.StockCountLineId,
                    line.StockCountId,
                    line.LotId,
                    lot.BatchCode,
                    lot.Item.ItemCode,
                    lot.Item.ItemName,
                    line.BinId,
                    bin.BinCode,
                    line.SystemQuantity,
                    line.CountedQuantity,
                    count.Status
                });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new
                {
                    Message = "Database error while adding stock count line.",
                    Error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        // PUT: api/stock-counts/{id}/lines/{lineId}
        [HttpPut("{id:long}/lines/{lineId:long}")]
        public async Task<IActionResult> RecordStockCount(
            long id,
            long lineId,
            [FromBody] RecordStockCountDto dto)
        {
            var count = await _context.StockCounts
                .FirstOrDefaultAsync(sc => sc.StockCountId == id);

            if (count == null)
                return NotFound("Stock count not found.");

            if (count.Status != "OPEN" &&
                count.Status != "IN_PROGRESS")
            {
                return BadRequest(
                    "Physical counts can only be recorded for OPEN or IN_PROGRESS stock counts.");
            }

            var line = await _context.StockCountLines
                .FirstOrDefaultAsync(line =>
                    line.StockCountLineId == lineId &&
                    line.StockCountId == id);

            if (line == null)
                return NotFound("Stock count line not found.");

            if (dto.CountedQuantity < 0)
                return BadRequest("Counted quantity cannot be negative.");

            if (dto.CountedBy.HasValue)
            {
                var userExists = await _context.AppUsers
                    .AnyAsync(u => u.UserId == dto.CountedBy.Value);

                if (!userExists)
                    return BadRequest("Counted by user does not exist.");
            }

            line.CountedQuantity = dto.CountedQuantity;
            line.CountedBy = dto.CountedBy;
            line.CountedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    line.StockCountLineId,
                    line.StockCountId,
                    line.LotId,
                    line.BinId,
                    line.SystemQuantity,
                    line.CountedQuantity,
                    line.CountedBy,
                    line.CountedAt,
                    DifferenceQuantity =
                        line.CountedQuantity.Value - line.SystemQuantity
                });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new
                {
                    Message = "Database error while recording stock count.",
                    Error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        // POST: api/stock-counts/{id}/complete
        [HttpPost("{id:long}/complete")]
        public async Task<IActionResult> CompleteStockCount(long id)
        {
            var count = await _context.StockCounts
                .Include(sc => sc.StockCountLines)
                .FirstOrDefaultAsync(sc => sc.StockCountId == id);

            if (count == null)
                return NotFound("Stock count not found.");

            if (count.Status != "IN_PROGRESS")
                return BadRequest(
                    "Only IN_PROGRESS stock counts can be completed.");

            if (!count.StockCountLines.Any())
                return BadRequest(
                    "Cannot complete a stock count without lines.");

            var uncountedLines = count.StockCountLines
                .Where(line => line.CountedQuantity == null)
                .ToList();

            if (uncountedLines.Any())
            {
                return BadRequest(new
                {
                    Message = "All stock count lines must have a physical count before completion.",
                    UncountedLines = uncountedLines.Select(
                        line => line.StockCountLineId)
                });
            }

            count.Status = "COMPLETED";

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Stock count completed successfully.",
                    count.StockCountId,
                    count.CountNumber,
                    count.Status,
                    TotalLines = count.StockCountLines.Count
                });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new
                {
                    Message = "Database error while completing stock count.",
                    Error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        // POST: api/stock-counts/{id}/post
        [HttpPost("{id:long}/post")]
        public async Task<IActionResult> PostStockCount(
            long id,
            [FromBody] PostStockCountDto dto)
        {
            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var count = await _context.StockCounts
                    .FromSqlInterpolated($@"
                        SELECT * FROM stock_counts
                        WHERE stock_count_id = {id}
                        FOR UPDATE")
                    .FirstOrDefaultAsync();

                if (count == null)
                    return NotFound("Stock count not found.");

                if (count.Status != "COMPLETED")
                    return BadRequest(
                        "Only COMPLETED stock counts can be posted.");

                var lines = await _context.StockCountLines
                    .Where(line => line.StockCountId == id)
                    .Include(line => line.Lot)
                        .ThenInclude(lot => lot.Item)
                    .ToListAsync();

                if (!lines.Any())
                    return BadRequest("Stock count has no lines.");

                if (lines.Any(line => line.CountedQuantity == null))
                    return BadRequest(
                        "All stock count lines must have a physical count.");

                if (dto.PostedBy.HasValue)
                {
                    var userExists = await _context.AppUsers
                        .AnyAsync(u => u.UserId == dto.PostedBy.Value);

                    if (!userExists)
                        return BadRequest("Posted by user does not exist.");
                }

                var transactionLines =
                    new List<InventoryTransactionLine>();

                foreach (var line in lines)
                {
                    var balance = await _context.InventoryBalances
                        .FromSqlInterpolated($@"
                            SELECT * FROM inventory_balances
                            WHERE lot_id = {line.LotId}
                            AND bin_id = {line.BinId}
                            AND stock_status = 'AVAILABLE'
                            FOR UPDATE")
                        .FirstOrDefaultAsync();

                    decimal currentQuantity =
                        balance?.QuantityOnHand ?? 0;

                    if (currentQuantity != line.SystemQuantity)
                    {
                        return Conflict(new
                        {
                            Message = "Inventory changed after the stock count was created. Please review the stock count before posting.",
                            line.StockCountLineId,
                            line.LotId,
                            line.BinId,
                            OriginalSystemQuantity = line.SystemQuantity,
                            CurrentSystemQuantity = currentQuantity
                        });
                    }

                    decimal difference =
                        line.CountedQuantity!.Value - currentQuantity;

                    if (difference == 0)
                        continue;

                    if (difference < 0)
                    {
                        if (balance == null ||
                            balance.QuantityOnHand + difference <
                            balance.QuantityReserved)
                        {
                            return BadRequest(new
                            {
                                Message = "Adjustment would reduce inventory below its reserved quantity.",
                                line.StockCountLineId,
                                line.LotId,
                                line.BinId
                            });
                        }

                        balance.QuantityOnHand += difference;
                        balance.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        if (balance == null)
                        {
                            balance = new InventoryBalance
                            {
                                LotId = line.LotId,
                                BinId = line.BinId,
                                StockStatus = "AVAILABLE",
                                QuantityOnHand = difference,
                                QuantityReserved = 0,
                                UpdatedAt = DateTime.UtcNow
                            };

                            _context.InventoryBalances.Add(balance);
                        }
                        else
                        {
                            balance.QuantityOnHand += difference;
                            balance.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    transactionLines.Add(new InventoryTransactionLine
                    {
                        LotId = line.LotId,
                        FromBinId = difference < 0 ? line.BinId : null,
                        ToBinId = difference > 0 ? line.BinId : null,
                        Quantity = Math.Abs(difference),
                        UomId = line.Lot.Item.BaseUomId,
                        LineRemarks =
                            $"Stock count variance: {count.CountNumber}"
                    });
                }

                var inventoryTransaction = new InventoryTransaction
                {
                    TransactionNumber = $"SC-{Guid.NewGuid():N}",
                    TransactionType = "COUNT_VARIANCE",
                    TransactionDate = DateTime.UtcNow,
                    ReferenceNumber = count.CountNumber,
                    Remarks = $"Posted stock count: {count.CountNumber}",
                    Status = "POSTED",
                    CreatedBy = count.CreatedBy,
                    PostedBy = dto.PostedBy,
                    PostedAt = DateTime.UtcNow,
                    InventoryTransactionLines = transactionLines
                };

                _context.InventoryTransactions.Add(inventoryTransaction);

                count.Status = "POSTED";

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok(new
                {
                    Message = "Stock count posted successfully.",
                    count.StockCountId,
                    count.CountNumber,
                    count.Status,
                    InventoryTransactionNumber =
                        inventoryTransaction.TransactionNumber,
                    TotalLines = lines.Count,
                    AdjustedLines = transactionLines.Count
                });
            }
            catch (DbUpdateException ex)
            {
                await dbTransaction.RollbackAsync();

                return BadRequest(new
                {
                    Message = "Database error while posting stock count.",
                    Error = ex.InnerException?.Message ?? ex.Message
                });
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }
}
