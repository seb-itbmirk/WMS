
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.StockTransfers;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/stock-transfers")]
    [ApiController]
    [Authorize]
    public class StockTransfersController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public StockTransfersController(WmsDbContext context)
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

        private async Task<StockTransfer?> LockTransferAsync(long id)
        {
            return await _context.StockTransfers
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM stock_transfers
                    WHERE transfer_id = {id}
                    FOR UPDATE
                    """)
                .FirstOrDefaultAsync();
        }

        // ============================================
        // GET: api/stock-transfers
        // Permission: inventory.view
        // ============================================

        [Authorize(Policy = "Permission:inventory.view")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var warehouseIds = await GetAccessibleWarehouseIdsAsync();

            var transfers = await _context.StockTransfers
                .AsNoTracking()
                .Where(t =>
                    warehouseIds.Contains(t.SourceWarehouseId) ||
                    warehouseIds.Contains(t.DestinationWarehouseId))
                .OrderByDescending(t => t.TransferId)
                .Select(t => new
                {
                    t.TransferId,
                    t.TransferNumber,
                    t.SourceWarehouseId,
                    SourceWarehouse = t.SourceWarehouse.WarehouseName,
                    t.DestinationWarehouseId,
                    DestinationWarehouse = t.DestinationWarehouse.WarehouseName,
                    t.TransferDate,
                    t.Status,
                    t.RequestedBy,
                    t.DispatchedBy,
                    t.ReceivedBy,
                    t.DispatchedAt,
                    t.ReceivedAt,
                    Lines = t.StockTransferLines
                        .OrderBy(l => l.TransferLineId)
                        .Select(l => new
                        {
                            l.TransferLineId,
                            l.LotId,
                            l.Lot.BatchCode,
                            l.SourceBinId,
                            l.DestinationBinId,
                            l.RequestedQuantity,
                            l.DispatchedQuantity,
                            l.ReceivedQuantity,
                            l.UomId,
                            l.DiscrepancyReason
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(transfers);
        }

        // ============================================
        // GET: api/stock-transfers/{id}
        // Permission: inventory.view
        // ============================================

        [Authorize(Policy = "Permission:inventory.view")]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            if (id <= 0)
                return BadRequest("A valid transfer ID is required.");

            var warehouseIds = await GetAccessibleWarehouseIdsAsync();

            var transfer = await _context.StockTransfers
                .AsNoTracking()
                .Where(t =>
                    t.TransferId == id &&
                    (warehouseIds.Contains(t.SourceWarehouseId) ||
                     warehouseIds.Contains(t.DestinationWarehouseId)))
                .Select(t => new
                {
                    t.TransferId,
                    t.TransferNumber,
                    t.SourceWarehouseId,
                    SourceWarehouse = t.SourceWarehouse.WarehouseName,
                    t.DestinationWarehouseId,
                    DestinationWarehouse = t.DestinationWarehouse.WarehouseName,
                    t.TransferDate,
                    t.Status,
                    t.RequestedBy,
                    t.DispatchedBy,
                    t.ReceivedBy,
                    t.DispatchedAt,
                    t.ReceivedAt,
                    Lines = t.StockTransferLines
                        .OrderBy(l => l.TransferLineId)
                        .Select(l => new
                        {
                            l.TransferLineId,
                            l.LotId,
                            l.Lot.BatchCode,
                            l.SourceBinId,
                            l.DestinationBinId,
                            l.RequestedQuantity,
                            l.DispatchedQuantity,
                            l.ReceivedQuantity,
                            l.UomId,
                            l.DiscrepancyReason
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (transfer == null)
                return NotFound("Stock transfer not found.");

            return Ok(transfer);
        }

        // ============================================
        // POST: api/stock-transfers
        // Permission: inventory.transfer
        // ============================================

        [Authorize(Policy = "Permission:inventory.transfer")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateStockTransferDto dto)
        {
            if (dto == null)
                return BadRequest("Transfer details are required.");

            var currentUserId = GetCurrentUserId();

            if (!currentUserId.HasValue)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.TransferNumber))
                return BadRequest("Transfer number is required.");

            string transferNumber = dto.TransferNumber.Trim();

            if (transferNumber.Length > 50)
                return BadRequest(
                    "Transfer number cannot exceed 50 characters.");

            if (dto.SourceWarehouseId <= 0 ||
                dto.DestinationWarehouseId <= 0)
            {
                return BadRequest(
                    "Valid source and destination warehouses are required.");
            }

            if (dto.SourceWarehouseId == dto.DestinationWarehouseId)
            {
                return BadRequest(
                    "Source and destination warehouses must be different.");
            }

            if (dto.Lines == null || !dto.Lines.Any())
                return BadRequest(
                    "At least one transfer line is required.");

            if (dto.Lines.Any(l =>
                l.LotId <= 0 ||
                l.SourceBinId <= 0 ||
                l.UomId <= 0 ||
                l.RequestedQuantity <= 0))
            {
                return BadRequest(
                    "Every transfer line must contain valid lot, source bin, UOM, and positive requested quantity.");
            }

            if (dto.Lines
                .GroupBy(l => new { l.LotId, l.SourceBinId })
                .Any(g => g.Count() > 1))
            {
                return BadRequest(
                    "Duplicate lot and source-bin combinations are not allowed.");
            }

            if (!await HasWarehouseAccessAsync(dto.SourceWarehouseId) ||
                !await HasWarehouseAccessAsync(dto.DestinationWarehouseId))
            {
                return NotFound("One or both warehouses were not found.");
            }

            var numberExists = await _context.StockTransfers
                .AnyAsync(t => t.TransferNumber == transferNumber);

            if (numberExists)
                return Conflict("Transfer number already exists.");

            var sourceWarehouse = await _context.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w =>
                    w.WarehouseId == dto.SourceWarehouseId &&
                    w.IsActive);

            if (sourceWarehouse == null)
                return BadRequest(
                    "Source warehouse does not exist or is inactive.");

            var destinationWarehouse = await _context.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w =>
                    w.WarehouseId == dto.DestinationWarehouseId &&
                    w.IsActive);

            if (destinationWarehouse == null)
                return BadRequest(
                    "Destination warehouse does not exist or is inactive.");

            foreach (var line in dto.Lines)
            {
                var lot = await _context.InventoryLots
                    .Include(l => l.Item)
                    .FirstOrDefaultAsync(l => l.LotId == line.LotId);

                if (lot == null)
                {
                    return BadRequest(
                        $"Inventory lot ID {line.LotId} does not exist.");
                }

                var sourceBin = await _context.StorageBins
                    .Include(b => b.Zone)
                    .FirstOrDefaultAsync(b =>
                        b.BinId == line.SourceBinId &&
                        b.IsActive);

                if (sourceBin == null)
                {
                    return BadRequest(
                        $"Source bin ID {line.SourceBinId} does not exist or is inactive.");
                }

                if (sourceBin.Zone == null ||
                    sourceBin.Zone.WarehouseId != dto.SourceWarehouseId)
                {
                    return BadRequest(
                        $"Source bin {sourceBin.BinCode} does not belong to the source warehouse.");
                }

                var uom = await _context.UnitsOfMeasures
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u =>
                        u.UomId == line.UomId &&
                        u.IsActive);

                if (uom == null)
                {
                    return BadRequest(
                        $"UOM ID {line.UomId} does not exist or is inactive.");
                }

                if (line.UomId != lot.Item.BaseUomId)
                {
                    return BadRequest(
                        $"Transfer line for lot {lot.BatchCode} must use the item's base unit of measure.");
                }
            }

            var transfer = new StockTransfer
            {
                TransferNumber = transferNumber,
                SourceWarehouseId = dto.SourceWarehouseId,
                DestinationWarehouseId = dto.DestinationWarehouseId,
                TransferDate = DateTime.UtcNow,
                Status = "DRAFT",
                RequestedBy = currentUserId.Value
            };

            foreach (var line in dto.Lines)
            {
                transfer.StockTransferLines.Add(
                    new StockTransferLine
                    {
                        LotId = line.LotId,
                        SourceBinId = line.SourceBinId,
                        RequestedQuantity = line.RequestedQuantity,
                        DispatchedQuantity = 0,
                        ReceivedQuantity = 0,
                        UomId = line.UomId
                    });
            }

            _context.StockTransfers.Add(transfer);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(
                    "Unable to create transfer. Check whether the transfer number already exists.");
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = transfer.TransferId },
                new
                {
                    message = "Stock transfer created successfully.",
                    transferId = transfer.TransferId,
                    transfer.TransferNumber,
                    transfer.Status
                });
        }

        // ============================================
        // POST: api/stock-transfers/{id}/dispatch
        // Permission: inventory.transfer
        // ============================================

        [Authorize(Policy = "Permission:inventory.transfer")]
        [HttpPost("{id:long}/dispatch")]
        public async Task<IActionResult> Dispatch(
            long id,
            [FromBody] DispatchStockTransferDto dto)
        {
            if (id <= 0)
                return BadRequest("A valid transfer ID is required.");

            if (dto == null)
                return BadRequest("Dispatch details are required.");

            var currentUserId = GetCurrentUserId();

            if (!currentUserId.HasValue)
                return Unauthorized();

            if (dto.Lines == null || !dto.Lines.Any())
                return BadRequest("Dispatch lines are required.");

            if (dto.Lines.Any(l =>
                l.TransferLineId <= 0 ||
                l.DispatchedQuantity <= 0))
            {
                return BadRequest(
                    "Every dispatch line must have a valid transfer line ID and positive dispatched quantity.");
            }

            if (dto.Lines
                .GroupBy(l => l.TransferLineId)
                .Any(g => g.Count() > 1))
            {
                return BadRequest(
                    "Duplicate transfer line IDs are not allowed.");
            }

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var transfer = await LockTransferAsync(id);

                if (transfer == null)
                    return NotFound("Stock transfer not found.");

                if (!await HasWarehouseAccessAsync(transfer.SourceWarehouseId))
                    return NotFound("Stock transfer not found.");

                await _context.Entry(transfer)
                    .Collection(t => t.StockTransferLines)
                    .LoadAsync();

                if (transfer.Status != "DRAFT")
                {
                    return BadRequest(
                        "Only DRAFT transfers can be dispatched.");
                }

                if (dto.Lines.Count != transfer.StockTransferLines.Count ||
                    transfer.StockTransferLines.Any(tl =>
                        !dto.Lines.Any(dl =>
                            dl.TransferLineId == tl.TransferLineId)))
                {
                    return BadRequest(
                        "All transfer lines must be included in the dispatch request.");
                }

                var now = DateTime.UtcNow;

                foreach (var dispatchLine in dto.Lines)
                {
                    var transferLine = transfer.StockTransferLines
                        .First(l =>
                            l.TransferLineId == dispatchLine.TransferLineId);

                    if (dispatchLine.DispatchedQuantity !=
                        transferLine.RequestedQuantity)
                    {
                        return BadRequest(
                            $"Dispatched quantity for transfer line {transferLine.TransferLineId} must equal its requested quantity.");
                    }

                    var lot = await _context.InventoryLots
                        .Include(l => l.Item)
                        .FirstOrDefaultAsync(l =>
                            l.LotId == transferLine.LotId);

                    if (lot == null)
                    {
                        return BadRequest(
                            $"Inventory lot ID {transferLine.LotId} does not exist.");
                    }

                    if (lot.QualityStatus != "APPROVED")
                    {
                        return BadRequest(
                            $"Lot {lot.BatchCode} is not approved for dispatch. Current quality status: {lot.QualityStatus}.");
                    }

                    if (lot.ExpiryDate.HasValue &&
                        lot.ExpiryDate.Value < DateOnly.FromDateTime(now))
                    {
                        return BadRequest(
                            $"Lot {lot.BatchCode} has expired.");
                    }

                    if (transferLine.UomId != lot.Item.BaseUomId)
                    {
                        return BadRequest(
                            $"Transfer line for lot {lot.BatchCode} must use the item's base unit of measure.");
                    }

                    var sourceBin = await _context.StorageBins
                        .Include(b => b.Zone)
                        .FirstOrDefaultAsync(b =>
                            b.BinId == transferLine.SourceBinId &&
                            b.IsActive);

                    if (sourceBin == null)
                    {
                        return BadRequest(
                            $"Source bin ID {transferLine.SourceBinId} does not exist or is inactive.");
                    }

                    if (sourceBin.Zone == null ||
                        sourceBin.Zone.WarehouseId != transfer.SourceWarehouseId)
                    {
                        return BadRequest(
                            $"Source bin {sourceBin.BinCode} does not belong to the source warehouse.");
                    }

                    var balance = await _context.InventoryBalances
                        .FromSqlInterpolated($"""
                            SELECT *
                            FROM inventory_balances
                            WHERE lot_id = {transferLine.LotId}
                              AND bin_id = {transferLine.SourceBinId}
                              AND stock_status = 'AVAILABLE'
                            FOR UPDATE
                            """)
                        .FirstOrDefaultAsync();

                    if (balance == null)
                    {
                        return BadRequest(
                            $"No AVAILABLE inventory found for lot {lot.BatchCode} in source bin {sourceBin.BinCode}.");
                    }

                    decimal availableQuantity =
                        balance.QuantityOnHand - balance.QuantityReserved;

                    if (availableQuantity < dispatchLine.DispatchedQuantity)
                    {
                        return BadRequest(
                            $"Insufficient available inventory for lot {lot.BatchCode}. Available: {availableQuantity}, requested: {dispatchLine.DispatchedQuantity}.");
                    }

                    balance.QuantityOnHand -= dispatchLine.DispatchedQuantity;
                    balance.UpdatedAt = now;

                    transferLine.DispatchedQuantity =
                        dispatchLine.DispatchedQuantity;
                }

                var inventoryTransaction = new InventoryTransaction
                {
                    TransactionNumber = "TRF-" + Guid.NewGuid().ToString("N"),
                    TransactionType = "TRANSFER",
                    TransactionDate = now,
                    ReferenceNumber = transfer.TransferNumber,
                    Remarks = "Stock transfer dispatch",
                    Status = "POSTED",
                    CreatedBy = currentUserId.Value,
                    PostedBy = currentUserId.Value,
                    PostedAt = now
                };

                foreach (var dispatchLine in dto.Lines)
                {
                    var transferLine = transfer.StockTransferLines
                        .First(l =>
                            l.TransferLineId == dispatchLine.TransferLineId);

                    inventoryTransaction.InventoryTransactionLines.Add(
                        new InventoryTransactionLine
                        {
                            LotId = transferLine.LotId,
                            FromBinId = transferLine.SourceBinId,
                            ToBinId = null,
                            Quantity = dispatchLine.DispatchedQuantity,
                            UomId = transferLine.UomId,
                            LineRemarks =
                                $"Dispatch for transfer {transfer.TransferNumber}"
                        });
                }

                _context.InventoryTransactions.Add(inventoryTransaction);

                transfer.Status = "DISPATCHED";
                transfer.DispatchedBy = currentUserId.Value;
                transfer.DispatchedAt = now;

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok(new
                {
                    message = "Stock transfer dispatched successfully.",
                    transferId = transfer.TransferId,
                    transferNumber = transfer.TransferNumber,
                    transferStatus = transfer.Status,
                    inventoryTransactionNumber =
                        inventoryTransaction.TransactionNumber,
                    inventoryTransactionType =
                        inventoryTransaction.TransactionType,
                    inventoryTransactionStatus =
                        inventoryTransaction.Status,
                    dispatchedAt = transfer.DispatchedAt
                });
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        // ============================================
        // POST: api/stock-transfers/{id}/receive
        // Permission: inventory.transfer
        // ============================================

        [Authorize(Policy = "Permission:inventory.transfer")]
        [HttpPost("{id:long}/receive")]
        public async Task<IActionResult> Receive(
            long id,
            [FromBody] ReceiveStockTransferDto dto)
        {
            if (id <= 0)
                return BadRequest("A valid transfer ID is required.");

            if (dto == null)
                return BadRequest("Receiving details are required.");

            var currentUserId = GetCurrentUserId();

            if (!currentUserId.HasValue)
                return Unauthorized();

            if (dto.Lines == null || !dto.Lines.Any())
                return BadRequest("Receiving lines are required.");

            if (dto.Lines.Any(l =>
                l.TransferLineId <= 0 ||
                l.DestinationBinId <= 0 ||
                l.ReceivedQuantity <= 0))
            {
                return BadRequest(
                    "Every receiving line must have valid IDs and a positive received quantity.");
            }

            if (dto.Lines
                .GroupBy(l => l.TransferLineId)
                .Any(g => g.Count() > 1))
            {
                return BadRequest(
                    "Duplicate transfer line IDs are not allowed.");
            }

            foreach (var line in dto.Lines)
            {
                if (line.DiscrepancyReason?.Length > 1000)
                {
                    return BadRequest(
                        "Discrepancy reason cannot exceed 1000 characters.");
                }
            }

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var transfer = await LockTransferAsync(id);

                if (transfer == null)
                    return NotFound("Stock transfer not found.");

                if (!await HasWarehouseAccessAsync(transfer.DestinationWarehouseId))
                    return NotFound("Stock transfer not found.");

                await _context.Entry(transfer)
                    .Collection(t => t.StockTransferLines)
                    .LoadAsync();

                if (transfer.Status != "DISPATCHED" &&
                    transfer.Status != "PARTIALLY_RECEIVED")
                {
                    return BadRequest(
                        "Only DISPATCHED or PARTIALLY_RECEIVED transfers can be received.");
                }

                // Validate every receiving line before changing inventory.
                foreach (var receiveLine in dto.Lines)
                {
                    var transferLine = transfer.StockTransferLines
                        .FirstOrDefault(l =>
                            l.TransferLineId == receiveLine.TransferLineId);

                    if (transferLine == null)
                    {
                        return BadRequest(
                            $"Transfer line ID {receiveLine.TransferLineId} does not belong to this transfer.");
                    }

                    decimal remainingQuantity =
                        transferLine.DispatchedQuantity -
                        transferLine.ReceivedQuantity;

                    if (receiveLine.ReceivedQuantity > remainingQuantity)
                    {
                        return BadRequest(
                            $"Received quantity exceeds the remaining dispatched quantity for transfer line {transferLine.TransferLineId}. Remaining: {remainingQuantity}.");
                    }

                    var destinationBin = await _context.StorageBins
                        .Include(b => b.Zone)
                        .FirstOrDefaultAsync(b =>
                            b.BinId == receiveLine.DestinationBinId &&
                            b.IsActive);

                    if (destinationBin == null)
                    {
                        return BadRequest(
                            $"Destination bin ID {receiveLine.DestinationBinId} does not exist or is inactive.");
                    }

                    if (destinationBin.Zone == null ||
                        destinationBin.Zone.WarehouseId !=
                        transfer.DestinationWarehouseId)
                    {
                        return BadRequest(
                            $"Destination bin {destinationBin.BinCode} does not belong to the destination warehouse.");
                    }

                    if (transferLine.DestinationBinId.HasValue &&
                        transferLine.DestinationBinId.Value !=
                        receiveLine.DestinationBinId)
                    {
                        return BadRequest(
                            $"Transfer line {transferLine.TransferLineId} has already been assigned to another destination bin.");
                    }

                    var lot = await _context.InventoryLots
                        .Include(l => l.Item)
                        .FirstOrDefaultAsync(l =>
                            l.LotId == transferLine.LotId);

                    if (lot == null)
                    {
                        return BadRequest(
                            $"Inventory lot ID {transferLine.LotId} does not exist.");
                    }

                    if (lot.QualityStatus != "APPROVED")
                    {
                        return BadRequest(
                            $"Lot {lot.BatchCode} is not approved for receiving. Current quality status: {lot.QualityStatus}.");
                    }

                    if (lot.ExpiryDate.HasValue &&
                        lot.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
                    {
                        return BadRequest(
                            $"Lot {lot.BatchCode} has expired.");
                    }

                    if (transferLine.UomId != lot.Item.BaseUomId)
                    {
                        return BadRequest(
                            $"Transfer line for lot {lot.BatchCode} must use the item's base unit of measure.");
                    }
                }

                var balanceCache =
                    new Dictionary<(long LotId, long BinId), InventoryBalance>();

                var now = DateTime.UtcNow;

                var inventoryTransaction = new InventoryTransaction
                {
                    TransactionNumber = "TRR-" + Guid.NewGuid().ToString("N"),
                    TransactionType = "TRANSFER",
                    TransactionDate = now,
                    ReferenceNumber = transfer.TransferNumber,
                    Remarks = "Stock transfer receiving",
                    Status = "POSTED",
                    CreatedBy = currentUserId.Value,
                    PostedBy = currentUserId.Value,
                    PostedAt = now
                };

                foreach (var receiveLine in dto.Lines)
                {
                    var transferLine = transfer.StockTransferLines
                        .First(l =>
                            l.TransferLineId == receiveLine.TransferLineId);

                    var key = (
                        transferLine.LotId,
                        receiveLine.DestinationBinId);

                    if (!balanceCache.TryGetValue(key, out var balance))
                    {
                        balance = await _context.InventoryBalances
                            .FromSqlInterpolated($"""
                                SELECT *
                                FROM inventory_balances
                                WHERE lot_id = {transferLine.LotId}
                                  AND bin_id = {receiveLine.DestinationBinId}
                                  AND stock_status = 'AVAILABLE'
                                FOR UPDATE
                                """)
                            .FirstOrDefaultAsync();

                        if (balance == null)
                        {
                            balance = new InventoryBalance
                            {
                                LotId = transferLine.LotId,
                                BinId = receiveLine.DestinationBinId,
                                StockStatus = "AVAILABLE",
                                QuantityOnHand = 0,
                                QuantityReserved = 0,
                                UpdatedAt = now
                            };

                            _context.InventoryBalances.Add(balance);
                        }

                        balanceCache[key] = balance;
                    }

                    balance.QuantityOnHand += receiveLine.ReceivedQuantity;
                    balance.UpdatedAt = now;

                    transferLine.ReceivedQuantity +=
                        receiveLine.ReceivedQuantity;

                    transferLine.DestinationBinId =
                        receiveLine.DestinationBinId;

                    if (!string.IsNullOrWhiteSpace(
                        receiveLine.DiscrepancyReason))
                    {
                        transferLine.DiscrepancyReason =
                            receiveLine.DiscrepancyReason.Trim();
                    }

                    inventoryTransaction.InventoryTransactionLines.Add(
                        new InventoryTransactionLine
                        {
                            LotId = transferLine.LotId,
                            FromBinId = null,
                            ToBinId = receiveLine.DestinationBinId,
                            Quantity = receiveLine.ReceivedQuantity,
                            UomId = transferLine.UomId,
                            LineRemarks =
                                $"Receiving for transfer {transfer.TransferNumber}"
                        });
                }

                bool allLinesReceived = transfer.StockTransferLines
                    .All(l => l.ReceivedQuantity >= l.DispatchedQuantity);

                transfer.Status = allLinesReceived
                    ? "RECEIVED"
                    : "PARTIALLY_RECEIVED";

                transfer.ReceivedBy = currentUserId.Value;
                transfer.ReceivedAt = now;

                _context.InventoryTransactions.Add(inventoryTransaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok(new
                {
                    message = "Stock transfer received successfully.",
                    transferId = transfer.TransferId,
                    transferNumber = transfer.TransferNumber,
                    transferStatus = transfer.Status,
                    inventoryTransactionNumber =
                        inventoryTransaction.TransactionNumber,
                    inventoryTransactionType =
                        inventoryTransaction.TransactionType,
                    inventoryTransactionStatus =
                        inventoryTransaction.Status,
                    receivedAt = transfer.ReceivedAt,
                    Lines = transfer.StockTransferLines.Select(l => new
                    {
                        l.TransferLineId,
                        l.LotId,
                        l.DispatchedQuantity,
                        l.ReceivedQuantity,
                        RemainingQuantity =
                            l.DispatchedQuantity - l.ReceivedQuantity,
                        l.DestinationBinId
                    })
                });
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }
}
