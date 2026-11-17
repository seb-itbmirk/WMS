
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.StockAdjustments;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/stock-adjustments")]
    [ApiController]
    public class StockAdjustmentsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public StockAdjustmentsController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/stock-adjustments
        [HttpGet]
        public async Task<IActionResult> GetAdjustments()
        {
            var adjustments = await _context.StockAdjustments
                .Include(a => a.Warehouse)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.AdjustmentId,
                    a.AdjustmentNumber,
                    a.WarehouseId,
                    WarehouseName = a.Warehouse.WarehouseName,
                    a.AdjustmentReason,
                    a.Status,
                    a.CreatedBy,
                    a.CreatedAt,
                    a.PostedAt
                })
                .ToListAsync();

            return Ok(adjustments);
        }

        // GET: api/stock-adjustments/{id}
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetAdjustment(long id)
        {
            var adjustment = await _context.StockAdjustments
                .Include(a => a.Warehouse)
                .Include(a => a.StockAdjustmentLines)
                    .ThenInclude(l => l.Lot)
                        .ThenInclude(l => l.Item)
                .Include(a => a.StockAdjustmentLines)
                    .ThenInclude(l => l.Bin)
                .Where(a => a.AdjustmentId == id)
                .Select(a => new
                {
                    a.AdjustmentId,
                    a.AdjustmentNumber,
                    a.WarehouseId,
                    WarehouseName = a.Warehouse.WarehouseName,
                    a.AdjustmentReason,
                    a.Status,
                    a.CreatedBy,
                    a.CreatedAt,
                    a.PostedAt,

                    Lines = a.StockAdjustmentLines.Select(l => new
                    {
                        l.AdjustmentLineId,
                        l.LotId,
                        BatchCode = l.Lot.BatchCode,
                        ItemCode = l.Lot.Item.ItemCode,
                        ItemName = l.Lot.Item.ItemName,
                        l.BinId,
                        BinCode = l.Bin.BinCode,
                        l.SystemQuantity,
                        l.CountedQuantity,
                        l.DifferenceQuantity
                    })
                })
                .FirstOrDefaultAsync();

            if (adjustment == null)
                return NotFound(new { message = "Stock adjustment not found." });

            return Ok(adjustment);
        }


        // POST: api/stock-adjustments/{id}/lines
        [HttpPost("{id:long}/lines")]
        public async Task<IActionResult> AddAdjustmentLine(
            long id,
            [FromBody] AddStockAdjustmentLineDto dto)
        {
            if (dto.LotId <= 0 || dto.BinId <= 0)
                return BadRequest(new { message = "Valid lot and bin are required." });

            if (dto.CountedQuantity < 0)
                return BadRequest(new { message = "Counted quantity cannot be negative." });

            var adjustment = await _context.StockAdjustments
                .FirstOrDefaultAsync(a => a.AdjustmentId == id);

            if (adjustment == null)
                return NotFound(new { message = "Stock adjustment not found." });

            if (adjustment.Status != "DRAFT")
                return BadRequest(new { message = "Only DRAFT adjustments can be modified." });

            var bin = await _context.StorageBins
                .Include(b => b.Zone)
                .FirstOrDefaultAsync(b =>
                    b.BinId == dto.BinId &&
                    b.IsActive);

            if (bin == null)
                return BadRequest(new { message = "Bin does not exist or is inactive." });

            if (bin.Zone.WarehouseId != adjustment.WarehouseId)
                return BadRequest(new { message = "Selected bin does not belong to the adjustment warehouse." });

            var lot = await _context.InventoryLots
                .Include(l => l.Item)
                .FirstOrDefaultAsync(l => l.LotId == dto.LotId);

            if (lot == null)
                return BadRequest(new { message = "Inventory lot does not exist." });

            var existingLine = await _context.StockAdjustmentLines
                .AnyAsync(l =>
                    l.AdjustmentId == id &&
                    l.LotId == dto.LotId &&
                    l.BinId == dto.BinId);

            if (existingLine)
                return Conflict(new { message = "This lot and bin already exist in the adjustment." });

            var balance = await _context.InventoryBalances
                .FirstOrDefaultAsync(b =>
                    b.LotId == dto.LotId &&
                    b.BinId == dto.BinId &&
                    b.StockStatus == "AVAILABLE");

            decimal systemQuantity = balance?.QuantityOnHand ?? 0;

            var differenceQuantity = dto.CountedQuantity - systemQuantity;

            var adjustmentLine = new StockAdjustmentLine
            {
                AdjustmentId = id,
                LotId = dto.LotId,
                BinId = dto.BinId,
                SystemQuantity = systemQuantity,
                CountedQuantity = dto.CountedQuantity,
                DifferenceQuantity = differenceQuantity
            };

            _context.StockAdjustmentLines.Add(adjustmentLine);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Adjustment line added successfully.",
                adjustmentLine.AdjustmentLineId,
                adjustmentLine.AdjustmentId,
                adjustmentLine.LotId,
                ItemCode = lot.Item.ItemCode,
                ItemName = lot.Item.ItemName,
                lot.BatchCode,
                adjustmentLine.BinId,
                BinCode = bin.BinCode,
                adjustmentLine.SystemQuantity,
                adjustmentLine.CountedQuantity,
                adjustmentLine.DifferenceQuantity
            });
        }


        // POST: api/stock-adjustments/{id}/post
        [HttpPost("{id:long}/post")]
        public async Task<IActionResult> PostAdjustment(
            long id,
            [FromBody] PostStockAdjustmentDto dto)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Lock the adjustment header.
                var adjustment = await _context.StockAdjustments
                    .FromSqlInterpolated($@"
                        SELECT *
                        FROM stock_adjustments
                        WHERE adjustment_id = {id}
                        FOR UPDATE")
                    .FirstOrDefaultAsync();

                if (adjustment == null)
                    return NotFound(new { message = "Stock adjustment not found." });

                if (adjustment.Status != "DRAFT")
                    return BadRequest(new { message = "Only DRAFT adjustments can be posted." });

                var lines = await _context.StockAdjustmentLines
                    .Where(l => l.AdjustmentId == id)
                    .OrderBy(l => l.AdjustmentLineId)
                    .ToListAsync();

                if (!lines.Any())
                    return BadRequest(new { message = "Cannot post an adjustment without lines." });

                if (dto.PostedBy.HasValue)
                {
                    var userExists = await _context.AppUsers
                        .AnyAsync(u => u.UserId == dto.PostedBy.Value);

                    if (!userExists)
                        return BadRequest(new { message = "Posting user does not exist." });
                }

                var transactionLines = new List<InventoryTransactionLine>();

                foreach (var line in lines)
                {
                    // Lock the AVAILABLE inventory balance.
                    var balance = await _context.InventoryBalances
                        .FromSqlInterpolated($@"
                            SELECT *
                            FROM inventory_balances
                            WHERE lot_id = {line.LotId}
                              AND bin_id = {line.BinId}
                              AND stock_status = 'AVAILABLE'
                            FOR UPDATE")
                        .FirstOrDefaultAsync();

                    decimal currentSystemQuantity =
                        balance?.QuantityOnHand ?? 0;

                    // Prevent posting using an outdated system quantity.
                    if (currentSystemQuantity != line.SystemQuantity)
                    {
                        return Conflict(new
                        {
                            message = "Inventory changed after the adjustment count. Please recount this line.",
                            adjustmentLineId = line.AdjustmentLineId,
                            recordedSystemQuantity = line.SystemQuantity,
                            currentSystemQuantity
                        });
                    }

                    decimal difference =
                        line.CountedQuantity - currentSystemQuantity;

                    line.DifferenceQuantity = difference;

                    // No inventory movement is needed for zero difference.
                    if (difference == 0)
                        continue;

                    // Negative adjustment: deduct available stock.
                    if (difference < 0)
                    {
                        if (balance == null ||
                            balance.QuantityOnHand + difference < balance.QuantityReserved)
                        {
                            return BadRequest(new
                            {
                                message = "Adjustment would reduce stock below the reserved quantity.",
                                adjustmentLineId = line.AdjustmentLineId
                            });
                        }

                        balance.QuantityOnHand += difference;
                        balance.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Positive adjustment: add stock.
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

                    var lot = await _context.InventoryLots
                        .Include(l => l.Item)
                        .FirstOrDefaultAsync(l => l.LotId == line.LotId);

                    if (lot == null)
                        return BadRequest(new { message = "Inventory lot not found." });

                    transactionLines.Add(new InventoryTransactionLine
                    {
                        LotId = line.LotId,
                        FromBinId = difference < 0 ? line.BinId : null,
                        ToBinId = difference > 0 ? line.BinId : null,
                        Quantity = Math.Abs(difference),
                        UomId = lot.Item.BaseUomId,
                        LineRemarks = "Stock adjustment"
                    });
                }

                // Create the inventory transaction.
                var inventoryTransaction = new InventoryTransaction
                {
                    TransactionNumber =
                        $"ADJ-{Guid.NewGuid():N}",
                    TransactionType = "ADJUSTMENT",
                    TransactionDate = DateTime.UtcNow,
                    ReferenceNumber = adjustment.AdjustmentNumber,
                    Remarks = adjustment.AdjustmentReason,
                    Status = "POSTED",
                    CreatedBy = adjustment.CreatedBy,
                    PostedBy = dto.PostedBy,
                    PostedAt = DateTime.UtcNow,
                    InventoryTransactionLines = transactionLines
                };

                _context.InventoryTransactions.Add(inventoryTransaction);

                // Mark adjustment as posted.
                adjustment.Status = "POSTED";
                adjustment.PostedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Stock adjustment posted successfully.",
                    adjustmentId = adjustment.AdjustmentId,
                    adjustmentNumber = adjustment.AdjustmentNumber,
                    adjustmentStatus = adjustment.Status,
                    inventoryTransactionNumber = inventoryTransaction.TransactionNumber,
                    inventoryTransactionType = inventoryTransaction.TransactionType,
                    inventoryTransactionStatus = inventoryTransaction.Status,
                    postedAt = adjustment.PostedAt,
                    lines = lines.Select(l => new
                    {
                        l.AdjustmentLineId,
                        l.LotId,
                        l.BinId,
                        l.SystemQuantity,
                        l.CountedQuantity,
                        l.DifferenceQuantity
                    })
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // PUT: api/stock-adjustments/{id}/lines/{lineId}
        [HttpPut("{id:long}/lines/{lineId:long}")]
        public async Task<IActionResult> UpdateAdjustmentLine(
            long id,
            long lineId,
            [FromBody] UpdateStockAdjustmentLineDto dto)
        {
            if (dto.LotId <= 0 || dto.BinId <= 0)
                return BadRequest(new { message = "Valid lot and bin are required." });

            if (dto.CountedQuantity < 0)
                return BadRequest(new { message = "Counted quantity cannot be negative." });

            // Find the adjustment.
            var adjustment = await _context.StockAdjustments
                .FirstOrDefaultAsync(a => a.AdjustmentId == id);

            if (adjustment == null)
                return NotFound(new { message = "Stock adjustment not found." });

            if (adjustment.Status != "DRAFT")
                return BadRequest(new
                {
                    message = "Only DRAFT adjustments can be modified."
                });

            // Find the adjustment line.
            var line = await _context.StockAdjustmentLines
                .FirstOrDefaultAsync(l =>
                    l.AdjustmentLineId == lineId &&
                    l.AdjustmentId == id);

            if (line == null)
                return NotFound(new { message = "Adjustment line not found." });

            // Validate the destination bin.
            var bin = await _context.StorageBins
                .Include(b => b.Zone)
                .FirstOrDefaultAsync(b =>
                    b.BinId == dto.BinId &&
                    b.IsActive);

            if (bin == null)
                return BadRequest(new
                {
                    message = "Bin does not exist or is inactive."
                });

            if (bin.Zone.WarehouseId != adjustment.WarehouseId)
                return BadRequest(new
                {
                    message = "Selected bin does not belong to the adjustment warehouse."
                });

            // Validate the inventory lot.
            var lot = await _context.InventoryLots
                .Include(l => l.Item)
                .FirstOrDefaultAsync(l => l.LotId == dto.LotId);

            if (lot == null)
                return BadRequest(new { message = "Inventory lot does not exist." });

            // Prevent duplicate lot/bin lines in the same adjustment.
            var duplicateLine = await _context.StockAdjustmentLines
                .AnyAsync(l =>
                    l.AdjustmentId == id &&
                    l.AdjustmentLineId != lineId &&
                    l.LotId == dto.LotId &&
                    l.BinId == dto.BinId);

            if (duplicateLine)
                return Conflict(new
                {
                    message = "This lot and bin already exist in another adjustment line."
                });

            // Retrieve the latest AVAILABLE inventory quantity.
            var balance = await _context.InventoryBalances
                .FirstOrDefaultAsync(b =>
                    b.LotId == dto.LotId &&
                    b.BinId == dto.BinId &&
                    b.StockStatus == "AVAILABLE");

            decimal systemQuantity = balance?.QuantityOnHand ?? 0;

            // Update the adjustment line.
            line.LotId = dto.LotId;
            line.BinId = dto.BinId;
            line.SystemQuantity = systemQuantity;
            line.CountedQuantity = dto.CountedQuantity;
            line.DifferenceQuantity = dto.CountedQuantity - systemQuantity;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Adjustment line updated successfully.",
                line.AdjustmentLineId,
                line.AdjustmentId,
                line.LotId,
                ItemCode = lot.Item.ItemCode,
                ItemName = lot.Item.ItemName,
                lot.BatchCode,
                line.BinId,
                BinCode = bin.BinCode,
                line.SystemQuantity,
                line.CountedQuantity,
                line.DifferenceQuantity
            });
        }



        // POST: api/stock-adjustments
        [HttpPost]
        public async Task<IActionResult> CreateAdjustment(
            [FromBody] CreateStockAdjustmentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AdjustmentNumber))
                return BadRequest(new { message = "Adjustment number is required." });

            if (string.IsNullOrWhiteSpace(dto.AdjustmentReason))
                return BadRequest(new { message = "Adjustment reason is required." });

            if (dto.WarehouseId <= 0)
                return BadRequest(new { message = "Valid warehouse is required." });

            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w =>
                    w.WarehouseId == dto.WarehouseId &&
                    w.IsActive);

            if (warehouse == null)
                return BadRequest(new { message = "Warehouse does not exist or is inactive." });

            var numberExists = await _context.StockAdjustments
                .AnyAsync(a => a.AdjustmentNumber == dto.AdjustmentNumber);

            if (numberExists)
                return Conflict(new { message = "Adjustment number already exists." });

            if (dto.CreatedBy.HasValue)
            {
                var userExists = await _context.AppUsers
                    .AnyAsync(u => u.UserId == dto.CreatedBy.Value);

                if (!userExists)
                    return BadRequest(new { message = "Created by user does not exist." });
            }

            var adjustment = new StockAdjustment
            {
                AdjustmentNumber = dto.AdjustmentNumber.Trim(),
                WarehouseId = dto.WarehouseId,
                AdjustmentReason = dto.AdjustmentReason.Trim(),
                Status = "DRAFT",
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockAdjustments.Add(adjustment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetAdjustment),
                new { id = adjustment.AdjustmentId },
                new
                {
                    message = "Stock adjustment created successfully.",
                    adjustment.AdjustmentId,
                    adjustment.AdjustmentNumber,
                    adjustment.WarehouseId,
                    adjustment.AdjustmentReason,
                    adjustment.Status,
                    adjustment.CreatedAt
                });
        }
    }
}
