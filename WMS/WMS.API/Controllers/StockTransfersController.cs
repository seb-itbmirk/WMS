
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;
using WMS.API.DTOs.StockTransfers;

namespace WMS.API.Controllers
{
    [Route("api/stock-transfers")]
    [ApiController]
    public class StockTransfersController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public StockTransfersController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/stock-transfers
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var transfers = await _context.StockTransfers
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Include(t => t.StockTransferLines)
                    .ThenInclude(l => l.Lot)
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

                    Lines = t.StockTransferLines.Select(l => new
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
                })
                .ToListAsync();

            return Ok(transfers);
        }

        // GET: api/stock-transfers/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var transfer = await _context.StockTransfers
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Include(t => t.StockTransferLines)
                    .ThenInclude(l => l.Lot)
                .FirstOrDefaultAsync(t => t.TransferId == id);

            if (transfer == null)
                return NotFound("Stock transfer not found.");

            return Ok(new
            {
                transfer.TransferId,
                transfer.TransferNumber,
                transfer.SourceWarehouseId,
                SourceWarehouse = transfer.SourceWarehouse.WarehouseName,
                transfer.DestinationWarehouseId,
                DestinationWarehouse = transfer.DestinationWarehouse.WarehouseName,
                transfer.TransferDate,
                transfer.Status,
                transfer.RequestedBy,
                transfer.DispatchedBy,
                transfer.ReceivedBy,
                transfer.DispatchedAt,
                transfer.ReceivedAt,

                Lines = transfer.StockTransferLines.Select(l => new
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
            });
        }

        // POST: api/stock-transfers
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateStockTransferDto dto)
        {
            // Validate transfer number.
            if (string.IsNullOrWhiteSpace(dto.TransferNumber))
                return BadRequest("Transfer number is required.");

            bool numberExists = await _context.StockTransfers
                .AnyAsync(t => t.TransferNumber == dto.TransferNumber);

            if (numberExists)
                return BadRequest("Transfer number already exists.");

            // Validate source and destination.
            if (dto.SourceWarehouseId == dto.DestinationWarehouseId)
                return BadRequest(
                    "Source and destination warehouses must be different.");

            var sourceWarehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w =>
                    w.WarehouseId == dto.SourceWarehouseId &&
                    w.IsActive);

            if (sourceWarehouse == null)
                return BadRequest(
                    "Source warehouse does not exist or is inactive.");

            var destinationWarehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w =>
                    w.WarehouseId == dto.DestinationWarehouseId &&
                    w.IsActive);

            if (destinationWarehouse == null)
                return BadRequest(
                    "Destination warehouse does not exist or is inactive.");

            // Validate lines.
            if (dto.Lines == null || !dto.Lines.Any())
                return BadRequest(
                    "At least one transfer line is required.");

            // Validate requesting user.
            if (dto.RequestedBy.HasValue)
            {
                bool userExists = await _context.AppUsers
                    .AnyAsync(u => u.UserId == dto.RequestedBy.Value);

                if (!userExists)
                    return BadRequest("Requesting user does not exist.");
            }

            // Validate each transfer line.
            foreach (var line in dto.Lines)
            {
                if (line.RequestedQuantity <= 0)
                    return BadRequest(
                        "Requested quantity must be greater than zero.");

                var lot = await _context.InventoryLots
                    .FirstOrDefaultAsync(l => l.LotId == line.LotId);

                if (lot == null)
                    return BadRequest(
                        $"Inventory lot ID {line.LotId} does not exist.");

                var sourceBin = await _context.StorageBins
                    .Include(b => b.Zone)
                    .FirstOrDefaultAsync(b =>
                        b.BinId == line.SourceBinId &&
                        b.IsActive);

                if (sourceBin == null)
                    return BadRequest(
                        $"Source bin ID {line.SourceBinId} does not exist or is inactive.");

                if (sourceBin.Zone.WarehouseId != dto.SourceWarehouseId)
                    return BadRequest(
                        $"Source bin {sourceBin.BinCode} does not belong to the source warehouse.");

                var uom = await _context.UnitsOfMeasures
                    .FirstOrDefaultAsync(u =>
                        u.UomId == line.UomId &&
                        u.IsActive);

                if (uom == null)
                    return BadRequest(
                        $"UOM ID {line.UomId} does not exist or is inactive.");
            }

            // Create transfer.
            var transfer = new StockTransfer
            {
                TransferNumber = dto.TransferNumber,
                SourceWarehouseId = dto.SourceWarehouseId,
                DestinationWarehouseId = dto.DestinationWarehouseId,
                TransferDate = DateTime.UtcNow,
                Status = "DRAFT",
                RequestedBy = dto.RequestedBy
            };

            // Add transfer lines.
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

            await _context.SaveChangesAsync();

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

        // POST: api/stock-transfers/1/dispatch
        [HttpPost("{id:long}/dispatch")]
        public async Task<IActionResult> Dispatch(
            long id,
            [FromBody] DispatchStockTransferDto dto)
        {
            // Start database transaction.
            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            // Lock transfer header to prevent simultaneous dispatch.
            var transfer = await _context.StockTransfers
                .FromSqlInterpolated(
                    $"SELECT * FROM stock_transfers WHERE transfer_id = {id} FOR UPDATE")
                .FirstOrDefaultAsync();

            if (transfer == null)
                return NotFound("Stock transfer not found.");

            // Load transfer lines.
            await _context.Entry(transfer)
                .Collection(t => t.StockTransferLines)
                .LoadAsync();

            if (transfer.Status != "DRAFT")
                return BadRequest(
                    "Only DRAFT transfers can be dispatched.");

            if (dto.Lines == null || !dto.Lines.Any())
                return BadRequest("Dispatch lines are required.");

            // Prevent duplicate transfer line IDs.
            if (dto.Lines
                .GroupBy(l => l.TransferLineId)
                .Any(g => g.Count() > 1))
            {
                return BadRequest(
                    "Duplicate transfer line IDs are not allowed.");
            }

            // Require all transfer lines.
            if (dto.Lines.Count != transfer.StockTransferLines.Count ||
                transfer.StockTransferLines.Any(tl =>
                    !dto.Lines.Any(dl =>
                        dl.TransferLineId == tl.TransferLineId)))
            {
                return BadRequest(
                    "All transfer lines must be included in the dispatch request.");
            }

            // Validate dispatching user.
            if (dto.DispatchedBy.HasValue)
            {
                bool userExists = await _context.AppUsers
                    .AnyAsync(u => u.UserId == dto.DispatchedBy.Value);

                if (!userExists)
                    return BadRequest(
                        "Dispatching user does not exist.");
            }

            // Validate each dispatch line.
            foreach (var dispatchLine in dto.Lines)
            {
                var transferLine = transfer.StockTransferLines
                    .FirstOrDefault(l =>
                        l.TransferLineId == dispatchLine.TransferLineId);

                if (transferLine == null)
                    return BadRequest(
                        $"Transfer line ID {dispatchLine.TransferLineId} was not found.");

                if (dispatchLine.DispatchedQuantity <= 0)
                    return BadRequest(
                        "Dispatched quantity must be greater than zero.");

                if (dispatchLine.DispatchedQuantity !=
                    transferLine.RequestedQuantity)
                {
                    return BadRequest(
                        $"Dispatched quantity for transfer line {transferLine.TransferLineId} must equal its requested quantity.");
                }

                // Get lot and item.
                var lot = await _context.InventoryLots
                    .Include(l => l.Item)
                    .FirstOrDefaultAsync(l =>
                        l.LotId == transferLine.LotId);

                if (lot == null)
                    return BadRequest(
                        $"Inventory lot ID {transferLine.LotId} does not exist.");

                // Check quality status.
                if (lot.QualityStatus != "APPROVED")
                    return BadRequest(
                        $"Lot {lot.BatchCode} is not approved for dispatch. Current quality status: {lot.QualityStatus}.");

                // Check expiry date.
                if (lot.ExpiryDate.HasValue &&
                    lot.ExpiryDate.Value <
                    DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    return BadRequest(
                        $"Lot {lot.BatchCode} has expired.");
                }

                // Check source bin.
                var sourceBin = await _context.StorageBins
                    .Include(b => b.Zone)
                    .FirstOrDefaultAsync(b =>
                        b.BinId == transferLine.SourceBinId);

                if (sourceBin == null || !sourceBin.IsActive)
                    return BadRequest(
                        $"Source bin ID {transferLine.SourceBinId} does not exist or is inactive.");

                // Check bin belongs to source warehouse.
                if (sourceBin.Zone.WarehouseId !=
                    transfer.SourceWarehouseId)
                {
                    return BadRequest(
                        $"Source bin {sourceBin.BinCode} does not belong to the source warehouse.");
                }

                // Check item base UOM.
                if (transferLine.UomId != lot.Item.BaseUomId)
                    return BadRequest(
                        $"Transfer line for lot {lot.BatchCode} must use the item's base unit of measure.");

                // Lock inventory balance row.
                var balance = await _context.InventoryBalances
                    .FromSqlInterpolated(
                        $"SELECT * FROM inventory_balances WHERE lot_id = {transferLine.LotId} AND bin_id = {transferLine.SourceBinId} AND stock_status = 'AVAILABLE' FOR UPDATE")
                    .FirstOrDefaultAsync();

                if (balance == null)
                    return BadRequest(
                        $"No AVAILABLE inventory found for lot {lot.BatchCode} in source bin {sourceBin.BinCode}.");

                decimal availableQuantity =
                    balance.QuantityOnHand - balance.QuantityReserved;

                if (availableQuantity <
                    dispatchLine.DispatchedQuantity)
                {
                    return BadRequest(
                        $"Insufficient available inventory for lot {lot.BatchCode}. Available: {availableQuantity}, requested: {dispatchLine.DispatchedQuantity}.");
                }

                // Deduct inventory.
                balance.QuantityOnHand -=
                    dispatchLine.DispatchedQuantity;

                balance.UpdatedAt = DateTime.UtcNow;

                // Update transfer line.
                transferLine.DispatchedQuantity =
                    dispatchLine.DispatchedQuantity;
            }

            // Create posted inventory transaction.
            var now = DateTime.UtcNow;

            var inventoryTransaction = new InventoryTransaction
            {
                TransactionNumber =
                    "TRF-" + Guid.NewGuid().ToString("N"),

                TransactionType = "TRANSFER",
                TransactionDate = now,
                ReferenceNumber = transfer.TransferNumber,
                Remarks = "Stock transfer dispatch",
                Status = "POSTED",
                CreatedBy = dto.DispatchedBy,
                PostedBy = dto.DispatchedBy,
                PostedAt = now
            };

            // Add inventory transaction lines.
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

            // Update transfer header.
            transfer.Status = "DISPATCHED";
            transfer.DispatchedBy = dto.DispatchedBy;
            transfer.DispatchedAt = now;

            // Save all changes.
            await _context.SaveChangesAsync();

            // Commit transaction.
            await dbTransaction.CommitAsync();

            return Ok(new
            {
                message = "Stock transfer dispatched successfully.",
                transferId = transfer.TransferId,
                transferNumber = transfer.TransferNumber,
                transferStatus = transfer.Status,
                inventoryTransactionNumber = inventoryTransaction.TransactionNumber,
                inventoryTransactionType = inventoryTransaction.TransactionType,
                inventoryTransactionStatus = inventoryTransaction.Status,
                dispatchedAt = transfer.DispatchedAt
            });
        }

        // POST: api/stock-transfers/1/receive
        [HttpPost("{id:long}/receive")]
        public async Task<IActionResult> Receive(
            long id,
            [FromBody] ReceiveStockTransferDto dto)
        {
            // Start database transaction.
            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            // Lock transfer header.
            var transfer = await _context.StockTransfers
                .FromSqlInterpolated(
                    $"SELECT * FROM stock_transfers WHERE transfer_id = {id} FOR UPDATE")
                .FirstOrDefaultAsync();

            if (transfer == null)
                return NotFound("Stock transfer not found.");

            // Load transfer lines.
            await _context.Entry(transfer)
                .Collection(t => t.StockTransferLines)
                .LoadAsync();

            if (transfer.Status != "DISPATCHED" &&
                transfer.Status != "PARTIALLY_RECEIVED")
            {
                return BadRequest(
                    "Only DISPATCHED or PARTIALLY_RECEIVED transfers can be received.");
            }

            if (dto.Lines == null || !dto.Lines.Any())
                return BadRequest("Receiving lines are required.");

            // Prevent duplicate transfer line IDs.
            if (dto.Lines
                .GroupBy(l => l.TransferLineId)
                .Any(g => g.Count() > 1))
            {
                return BadRequest(
                    "Duplicate transfer line IDs are not allowed.");
            }

            // Validate receiving user.
            if (dto.ReceivedBy.HasValue)
            {
                bool userExists = await _context.AppUsers
                    .AnyAsync(u => u.UserId == dto.ReceivedBy.Value);

                if (!userExists)
                    return BadRequest(
                        "Receiving user does not exist.");
            }

            var now = DateTime.UtcNow;

            // Create posted inventory transaction.
            var inventoryTransaction = new InventoryTransaction
            {
                TransactionNumber =
                    "TRR-" + Guid.NewGuid().ToString("N"),

                TransactionType = "TRANSFER",
                TransactionDate = now,
                ReferenceNumber = transfer.TransferNumber,
                Remarks = "Stock transfer receiving",
                Status = "POSTED",
                CreatedBy = dto.ReceivedBy,
                PostedBy = dto.ReceivedBy,
                PostedAt = now
            };

            // Process each received line.
            foreach (var receiveLine in dto.Lines)
            {
                var transferLine = transfer.StockTransferLines
                    .FirstOrDefault(l =>
                        l.TransferLineId == receiveLine.TransferLineId);

                if (transferLine == null)
                    return BadRequest(
                        $"Transfer line ID {receiveLine.TransferLineId} does not belong to this transfer.");

                if (receiveLine.ReceivedQuantity <= 0)
                    return BadRequest(
                        "Received quantity must be greater than zero.");

                // Calculate remaining dispatched quantity.
                decimal remainingQuantity =
                    transferLine.DispatchedQuantity -
                    transferLine.ReceivedQuantity;

                if (receiveLine.ReceivedQuantity > remainingQuantity)
                {
                    return BadRequest(
                        $"Received quantity exceeds the remaining dispatched quantity for transfer line {transferLine.TransferLineId}. Remaining: {remainingQuantity}.");
                }

                // Validate destination bin.
                var destinationBin = await _context.StorageBins
                    .Include(b => b.Zone)
                    .FirstOrDefaultAsync(b =>
                        b.BinId == receiveLine.DestinationBinId);

                if (destinationBin == null ||
                    !destinationBin.IsActive)
                {
                    return BadRequest(
                        $"Destination bin ID {receiveLine.DestinationBinId} does not exist or is inactive.");
                }

                // Validate destination warehouse.
                if (destinationBin.Zone.WarehouseId !=
                    transfer.DestinationWarehouseId)
                {
                    return BadRequest(
                        $"Destination bin {destinationBin.BinCode} does not belong to the destination warehouse.");
                }

                // Keep the same destination bin for partial receipts.
                if (transferLine.DestinationBinId.HasValue &&
                    transferLine.DestinationBinId.Value !=
                    receiveLine.DestinationBinId)
                {
                    return BadRequest(
                        $"Transfer line {transferLine.TransferLineId} has already been assigned to another destination bin.");
                }

                // Load lot and item.
                var lot = await _context.InventoryLots
                    .Include(l => l.Item)
                    .FirstOrDefaultAsync(l =>
                        l.LotId == transferLine.LotId);

                if (lot == null)
                    return BadRequest(
                        $"Inventory lot ID {transferLine.LotId} does not exist.");

                // Recheck quality status.
                if (lot.QualityStatus != "APPROVED")
                {
                    return BadRequest(
                        $"Lot {lot.BatchCode} is not approved for receiving. Current quality status: {lot.QualityStatus}.");
                }

                // Recheck expiry date.
                if (lot.ExpiryDate.HasValue &&
                    lot.ExpiryDate.Value <
                    DateOnly.FromDateTime(now))
                {
                    return BadRequest(
                        $"Lot {lot.BatchCode} has expired.");
                }

                // Check item base UOM.
                if (transferLine.UomId != lot.Item.BaseUomId)
                    return BadRequest(
                        $"Transfer line for lot {lot.BatchCode} must use the item's base unit of measure.");

                // Lock existing destination balance.
                var balance = await _context.InventoryBalances
                    .FromSqlInterpolated(
                        $"SELECT * FROM inventory_balances WHERE lot_id = {transferLine.LotId} AND bin_id = {receiveLine.DestinationBinId} AND stock_status = 'AVAILABLE' FOR UPDATE")
                    .FirstOrDefaultAsync();

                if (balance == null)
                {
                    // Create a new destination balance.
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

                // Add received quantity to destination inventory.
                balance.QuantityOnHand +=
                    receiveLine.ReceivedQuantity;

                balance.UpdatedAt = now;

                // Update transfer line.
                transferLine.ReceivedQuantity +=
                    receiveLine.ReceivedQuantity;

                transferLine.DestinationBinId =
                    receiveLine.DestinationBinId;

                if (!string.IsNullOrWhiteSpace(
                    receiveLine.DiscrepancyReason))
                {
                    transferLine.DiscrepancyReason =
                        receiveLine.DiscrepancyReason;
                }

                // Create inventory transaction line.
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

            // Check whether all lines have been fully received.
            bool allLinesReceived = transfer.StockTransferLines
                .All(l =>
                    l.ReceivedQuantity >= l.DispatchedQuantity);

            transfer.Status = allLinesReceived
                ? "RECEIVED"
                : "PARTIALLY_RECEIVED";

            transfer.ReceivedBy = dto.ReceivedBy;
            transfer.ReceivedAt = now;

            // Save inventory transaction.
            _context.InventoryTransactions.Add(inventoryTransaction);

            // Save all receiving changes.
            await _context.SaveChangesAsync();

            // Commit database transaction.
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
    }
}
