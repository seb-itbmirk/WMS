
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.StockAdjustments;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/stock-adjustments")]
    [ApiController]
    [Authorize]
    public class StockAdjustmentsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public StockAdjustmentsController(WmsDbContext context)
        {
            _context = context;
        }

        // ============================================
        // AUTHORIZATION HELPERS
        // ============================================

        private long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return long.TryParse(userIdClaim, out var userId)
                ? userId
                : null;
        }

        private async Task<List<long>> GetAccessibleWarehouseIdsAsync()
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
                return new List<long>();

            return await _context.Database
                .SqlQuery<long>($"""
                    SELECT warehouse_id AS "Value"
                    FROM user_warehouse_access
                    WHERE user_id = {userId.Value}
                    """)
                .ToListAsync();
        }

        private async Task<bool> HasWarehouseAccessAsync(long warehouseId)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
                return false;

            return await _context.Database
                .SqlQuery<bool>($"""
                    SELECT EXISTS (
                        SELECT 1
                        FROM user_warehouse_access
                        WHERE user_id = {userId.Value}
                          AND warehouse_id = {warehouseId}
                    ) AS "Value"
                    """)
                .SingleAsync();
        }

        private async Task<StockAdjustment?> LockAdjustmentAsync(long id)
        {
            return await _context.StockAdjustments
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM stock_adjustments
                    WHERE adjustment_id = {id}
                    FOR UPDATE
                    """)
                .FirstOrDefaultAsync();
        }

        // ============================================
        // GET: api/stock-adjustments
        // Permission: inventory.view
        // ============================================

        [Authorize(Policy = "Permission:inventory.view")]
        [HttpGet]
        public async Task<IActionResult> GetAdjustments()
        {
            var warehouseIds = await GetAccessibleWarehouseIdsAsync();

            var adjustments = await _context.StockAdjustments
                .AsNoTracking()
                .Where(a => warehouseIds.Contains(a.WarehouseId))
                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.AdjustmentId)
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

        // ============================================
        // GET: api/stock-adjustments/{id}
        // Permission: inventory.view
        // ============================================

        [Authorize(Policy = "Permission:inventory.view")]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetAdjustment(long id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid adjustment ID is required."
                });
            }

            var warehouseIds = await GetAccessibleWarehouseIdsAsync();

            var adjustment = await _context.StockAdjustments
                .AsNoTracking()
                .Where(a =>
                    a.AdjustmentId == id &&
                    warehouseIds.Contains(a.WarehouseId))
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

                    Lines = a.StockAdjustmentLines
                        .OrderBy(l => l.AdjustmentLineId)
                        .Select(l => new
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
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (adjustment == null)
            {
                return NotFound(new
                {
                    message = "Stock adjustment not found."
                });
            }

            return Ok(adjustment);
        }

        // ============================================
        // POST: api/stock-adjustments
        // Permission: inventory.adjust
        // ============================================

        [Authorize(Policy = "Permission:inventory.adjust")]
        [HttpPost]
        public async Task<IActionResult> CreateAdjustment(
            [FromBody] CreateStockAdjustmentDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Adjustment details are required."
                });
            }

            var currentUserId = GetCurrentUserId();

            if (!currentUserId.HasValue)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.AdjustmentNumber))
            {
                return BadRequest(new
                {
                    message = "Adjustment number is required."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.AdjustmentReason))
            {
                return BadRequest(new
                {
                    message = "Adjustment reason is required."
                });
            }

            var adjustmentNumber = dto.AdjustmentNumber.Trim();
            var adjustmentReason = dto.AdjustmentReason.Trim();

            if (adjustmentNumber.Length > 50)
            {
                return BadRequest(new
                {
                    message = "Adjustment number cannot exceed 50 characters."
                });
            }

            if (adjustmentReason.Length > 200)
            {
                return BadRequest(new
                {
                    message = "Adjustment reason cannot exceed 200 characters."
                });
            }

            if (dto.WarehouseId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid warehouse is required."
                });
            }

            if (!await HasWarehouseAccessAsync(dto.WarehouseId))
            {
                return NotFound(new
                {
                    message = "Warehouse not found."
                });
            }

            var warehouseExists = await _context.Warehouses
                .AsNoTracking()
                .AnyAsync(w =>
                    w.WarehouseId == dto.WarehouseId &&
                    w.IsActive);

            if (!warehouseExists)
            {
                return BadRequest(new
                {
                    message = "Warehouse does not exist or is inactive."
                });
            }

            var numberExists = await _context.StockAdjustments
                .AnyAsync(a =>
                    a.AdjustmentNumber == adjustmentNumber);

            if (numberExists)
            {
                return Conflict(new
                {
                    message = "Adjustment number already exists."
                });
            }

            var adjustment = new StockAdjustment
            {
                AdjustmentNumber = adjustmentNumber,
                WarehouseId = dto.WarehouseId,
                AdjustmentReason = adjustmentReason,
                Status = "DRAFT",
                CreatedBy = currentUserId.Value,
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

        // ============================================
        // POST: api/stock-adjustments/{id}/lines
        // Permission: inventory.adjust
        // ============================================

        [Authorize(Policy = "Permission:inventory.adjust")]
        [HttpPost("{id:long}/lines")]
        public async Task<IActionResult> AddAdjustmentLine(
            long id,
            [FromBody] AddStockAdjustmentLineDto dto)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid adjustment ID is required."
                });
            }

            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Adjustment line details are required."
                });
            }

            if (dto.LotId <= 0 || dto.BinId <= 0)
            {
                return BadRequest(new
                {
                    message = "Valid lot and bin are required."
                });
            }

            if (dto.CountedQuantity < 0)
            {
                return BadRequest(new
                {
                    message = "Counted quantity cannot be negative."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var adjustment = await LockAdjustmentAsync(id);

                if (adjustment == null)
                {
                    return NotFound(new
                    {
                        message = "Stock adjustment not found."
                    });
                }

                if (!await HasWarehouseAccessAsync(adjustment.WarehouseId))
                {
                    return NotFound(new
                    {
                        message = "Stock adjustment not found."
                    });
                }

                if (adjustment.Status != "DRAFT")
                {
                    return BadRequest(new
                    {
                        message = "Only DRAFT adjustments can be modified."
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
                        message = "Bin does not exist or is inactive."
                    });
                }

                if (bin.Zone == null ||
                    bin.Zone.WarehouseId != adjustment.WarehouseId)
                {
                    return BadRequest(new
                    {
                        message = "Selected bin does not belong to the adjustment warehouse."
                    });
                }

                var lot = await _context.InventoryLots
                    .Include(l => l.Item)
                    .FirstOrDefaultAsync(l =>
                        l.LotId == dto.LotId);

                if (lot == null)
                {
                    return BadRequest(new
                    {
                        message = "Inventory lot does not exist."
                    });
                }

                var existingLine = await _context.StockAdjustmentLines
                    .AnyAsync(l =>
                        l.AdjustmentId == id &&
                        l.LotId == dto.LotId &&
                        l.BinId == dto.BinId);

                if (existingLine)
                {
                    return Conflict(new
                    {
                        message = "This lot and bin already exist in the adjustment."
                    });
                }

                var balance = await _context.InventoryBalances
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b =>
                        b.LotId == dto.LotId &&
                        b.BinId == dto.BinId &&
                        b.StockStatus == "AVAILABLE");

                decimal systemQuantity =
                    balance?.QuantityOnHand ?? 0m;

                var adjustmentLine = new StockAdjustmentLine
                {
                    AdjustmentId = id,
                    LotId = dto.LotId,
                    BinId = dto.BinId,
                    SystemQuantity = systemQuantity,
                    CountedQuantity = dto.CountedQuantity,
                    DifferenceQuantity =
                        dto.CountedQuantity - systemQuantity
                };

                _context.StockAdjustmentLines.Add(adjustmentLine);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

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
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ============================================
        // PUT: api/stock-adjustments/{id}/lines/{lineId}
        // Permission: inventory.adjust
        // ============================================

        [Authorize(Policy = "Permission:inventory.adjust")]
        [HttpPut("{id:long}/lines/{lineId:long}")]
        public async Task<IActionResult> UpdateAdjustmentLine(
            long id,
            long lineId,
            [FromBody] UpdateStockAdjustmentLineDto dto)
        {
            if (id <= 0 || lineId <= 0)
            {
                return BadRequest(new
                {
                    message = "Valid adjustment and line IDs are required."
                });
            }

            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Adjustment line details are required."
                });
            }

            if (dto.LotId <= 0 || dto.BinId <= 0)
            {
                return BadRequest(new
                {
                    message = "Valid lot and bin are required."
                });
            }

            if (dto.CountedQuantity < 0)
            {
                return BadRequest(new
                {
                    message = "Counted quantity cannot be negative."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var adjustment = await LockAdjustmentAsync(id);

                if (adjustment == null)
                {
                    return NotFound(new
                    {
                        message = "Stock adjustment not found."
                    });
                }

                if (!await HasWarehouseAccessAsync(adjustment.WarehouseId))
                {
                    return NotFound(new
                    {
                        message = "Stock adjustment not found."
                    });
                }

                if (adjustment.Status != "DRAFT")
                {
                    return BadRequest(new
                    {
                        message = "Only DRAFT adjustments can be modified."
                    });
                }

                var line = await _context.StockAdjustmentLines
                    .FirstOrDefaultAsync(l =>
                        l.AdjustmentLineId == lineId &&
                        l.AdjustmentId == id);

                if (line == null)
                {
                    return NotFound(new
                    {
                        message = "Adjustment line not found."
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
                        message = "Bin does not exist or is inactive."
                    });
                }

                if (bin.Zone == null ||
                    bin.Zone.WarehouseId != adjustment.WarehouseId)
                {
                    return BadRequest(new
                    {
                        message = "Selected bin does not belong to the adjustment warehouse."
                    });
                }

                var lot = await _context.InventoryLots
                    .Include(l => l.Item)
                    .FirstOrDefaultAsync(l =>
                        l.LotId == dto.LotId);

                if (lot == null)
                {
                    return BadRequest(new
                    {
                        message = "Inventory lot does not exist."
                    });
                }

                var duplicateLine = await _context.StockAdjustmentLines
                    .AnyAsync(l =>
                        l.AdjustmentId == id &&
                        l.AdjustmentLineId != lineId &&
                        l.LotId == dto.LotId &&
                        l.BinId == dto.BinId);

                if (duplicateLine)
                {
                    return Conflict(new
                    {
                        message = "This lot and bin already exist in another adjustment line."
                    });
                }

                var balance = await _context.InventoryBalances
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b =>
                        b.LotId == dto.LotId &&
                        b.BinId == dto.BinId &&
                        b.StockStatus == "AVAILABLE");

                decimal systemQuantity =
                    balance?.QuantityOnHand ?? 0m;

                line.LotId = dto.LotId;
                line.BinId = dto.BinId;
                line.SystemQuantity = systemQuantity;
                line.CountedQuantity = dto.CountedQuantity;
                line.DifferenceQuantity =
                    dto.CountedQuantity - systemQuantity;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

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
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ============================================
        // POST: api/stock-adjustments/{id}/post
        // Permission: inventory.adjust
        // ============================================

        [Authorize(Policy = "Permission:inventory.adjust")]
        [HttpPost("{id:long}/post")]
        public async Task<IActionResult> PostAdjustment(
            long id,
            [FromBody] PostStockAdjustmentDto dto)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid adjustment ID is required."
                });
            }

            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Posting details are required."
                });
            }

            var currentUserId = GetCurrentUserId();

            if (!currentUserId.HasValue)
                return Unauthorized();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var adjustment = await LockAdjustmentAsync(id);

                if (adjustment == null)
                {
                    return NotFound(new
                    {
                        message = "Stock adjustment not found."
                    });
                }

                if (!await HasWarehouseAccessAsync(adjustment.WarehouseId))
                {
                    return NotFound(new
                    {
                        message = "Stock adjustment not found."
                    });
                }

                if (adjustment.Status != "DRAFT")
                {
                    return BadRequest(new
                    {
                        message = "Only DRAFT adjustments can be posted."
                    });
                }

                var lines = await _context.StockAdjustmentLines
                    .Where(l => l.AdjustmentId == id)
                    .OrderBy(l => l.AdjustmentLineId)
                    .ToListAsync();

                if (!lines.Any())
                {
                    return BadRequest(new
                    {
                        message = "Cannot post an adjustment without lines."
                    });
                }

                var transactionLines =
                    new List<InventoryTransactionLine>();

                foreach (var line in lines)
                {
                    var lot = await _context.InventoryLots
                        .Include(l => l.Item)
                        .FirstOrDefaultAsync(l =>
                            l.LotId == line.LotId);

                    if (lot == null)
                    {
                        return BadRequest(new
                        {
                            message = "Inventory lot not found.",
                            adjustmentLineId = line.AdjustmentLineId
                        });
                    }

                    var binBelongsToWarehouse =
                        await _context.StorageBins
                            .AnyAsync(b =>
                                b.BinId == line.BinId &&
                                b.IsActive &&
                                b.Zone != null &&
                                b.Zone.WarehouseId == adjustment.WarehouseId);

                    if (!binBelongsToWarehouse)
                    {
                        return BadRequest(new
                        {
                            message = "An adjustment line bin is no longer active or does not belong to the adjustment warehouse.",
                            adjustmentLineId = line.AdjustmentLineId
                        });
                    }

                    var balance = await _context.InventoryBalances
                        .FromSqlInterpolated($"""
                            SELECT *
                            FROM inventory_balances
                            WHERE lot_id = {line.LotId}
                              AND bin_id = {line.BinId}
                              AND stock_status = 'AVAILABLE'
                            FOR UPDATE
                            """)
                        .FirstOrDefaultAsync();

                    decimal currentSystemQuantity =
                        balance?.QuantityOnHand ?? 0m;

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
                                message = "Adjustment would reduce stock below the reserved quantity.",
                                adjustmentLineId = line.AdjustmentLineId
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

                    transactionLines.Add(
                        new InventoryTransactionLine
                        {
                            LotId = line.LotId,
                            FromBinId = difference < 0
                                ? line.BinId
                                : null,
                            ToBinId = difference > 0
                                ? line.BinId
                                : null,
                            Quantity = Math.Abs(difference),
                            UomId = lot.Item.BaseUomId,
                            LineRemarks = "Stock adjustment"
                        });
                }

                var now = DateTime.UtcNow;

                var inventoryTransaction = new InventoryTransaction
                {
                    TransactionNumber = $"ADJ-{Guid.NewGuid():N}",
                    TransactionType = "ADJUSTMENT",
                    TransactionDate = now,
                    ReferenceNumber = adjustment.AdjustmentNumber,
                    Remarks = adjustment.AdjustmentReason,
                    Status = "POSTED",
                    CreatedBy = currentUserId.Value,
                    PostedBy = currentUserId.Value,
                    PostedAt = now,
                    InventoryTransactionLines = transactionLines
                };

                _context.InventoryTransactions.Add(inventoryTransaction);

                adjustment.Status = "POSTED";
                adjustment.PostedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Stock adjustment posted successfully.",
                    adjustmentId = adjustment.AdjustmentId,
                    adjustmentNumber = adjustment.AdjustmentNumber,
                    adjustmentStatus = adjustment.Status,
                    inventoryTransactionNumber =
                        inventoryTransaction.TransactionNumber,
                    inventoryTransactionType =
                        inventoryTransaction.TransactionType,
                    inventoryTransactionStatus =
                        inventoryTransaction.Status,
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
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
