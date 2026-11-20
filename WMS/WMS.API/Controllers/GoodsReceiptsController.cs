using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;
using WMS.API.DTOs.GoodsReceipts;

namespace WMS.API.Controllers
{
    [Route("api/goods-receipts")]
    [ApiController]
    public class GoodsReceiptsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public GoodsReceiptsController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/goods-receipts
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var receipts = await _context.GoodsReceipts
                .Include(r => r.Warehouse)
                .Include(r => r.PurchaseOrder)
                .Include(r => r.GoodsReceiptLines)
                    .ThenInclude(l => l.Item)
                .OrderByDescending(r => r.ReceiptDate)
                .Select(r => new
                {
                    r.ReceiptId,
                    r.ReceiptNumber,
                    r.PurchaseOrderId,
                    PoNumber = r.PurchaseOrder != null
                        ? r.PurchaseOrder.PoNumber : null,
                    r.WarehouseId,
                    WarehouseName = r.Warehouse.WarehouseName,
                    r.ReceiptDate,
                    r.DeliveryReference,
                    r.Status,
                    Lines = r.GoodsReceiptLines.Select(l => new
                    {
                        l.ReceiptLineId,
                        l.ItemId,
                        ItemCode = l.Item.ItemCode,
                        ItemName = l.Item.ItemName,
                        l.LotId,
                        BatchCode = l.Lot.BatchCode,
                        l.ReceivedQuantity,
                        l.UomId,
                        l.InspectionStatus,
                        l.PoLineId,
                        l.ReceivingBinId
                    })
                })
                .ToListAsync();

            return Ok(receipts);
        }


        // POST: api/goods-receipts/1/post
        [HttpPost("{id:long}/post")]
        public async Task<IActionResult> Post(long id)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                // Find receipt and its lines
                var receipt = await _context.GoodsReceipts
                    .Include(r => r.GoodsReceiptLines)
                        .ThenInclude(l => l.Item)
                    .Include(r => r.GoodsReceiptLines)
                        .ThenInclude(l => l.Lot)
                    .FirstOrDefaultAsync(r => r.ReceiptId == id);

                if (receipt == null)
                    return NotFound("Goods receipt not found.");

                if (receipt.Status != "DRAFT")
                    return BadRequest(
                        "Only DRAFT goods receipts can be posted.");

                if (receipt.GoodsReceiptLines.Count == 0)
                    return BadRequest(
                        "Goods receipt has no receipt lines.");

                // Validate all lines before posting
                foreach (var line in receipt.GoodsReceiptLines)
                {
                    if (line.ReceivedQuantity <= 0)
                        return BadRequest(
                            $"Invalid received quantity for receipt line {line.ReceiptLineId}.");

                    if (!line.ReceivingBinId.HasValue)
                        return BadRequest(
                            $"Receipt line {line.ReceiptLineId} has no receiving bin.");

                    if (line.Item == null || !line.Item.IsActive)
                        return BadRequest(
                            $"Item ID {line.ItemId} does not exist or is inactive.");

                    if (line.Lot == null || line.Lot.ItemId != line.ItemId)
                        return BadRequest(
                            $"Inventory lot {line.LotId} does not belong to item {line.ItemId}.");

                    // Require receipt quantity to use the item's base UOM
                    if (line.UomId != line.Item.BaseUomId)
                        return BadRequest(
                            $"Receipt line {line.ReceiptLineId} must use the item's base UOM.");

                    // Validate receiving bin
                    var receivingBin = await _context.StorageBins
                        .Include(b => b.Zone)
                        .FirstOrDefaultAsync(b =>
                            b.BinId == line.ReceivingBinId.Value &&
                            b.IsActive);

                    if (receivingBin == null)
                        return BadRequest(
                            $"Receiving bin {line.ReceivingBinId} does not exist or is inactive.");

                    if (receivingBin.Zone.WarehouseId != receipt.WarehouseId)
                        return BadRequest(
                            $"Receiving bin {receivingBin.BinCode} does not belong to the receipt warehouse.");

                    string inspectionStatus =
                        line.InspectionStatus.Trim().ToUpper();

                    string[] allowedStatuses =
                    {
                "PENDING",
                "APPROVED",
                "REJECTED",
                "QUARANTINED"
            };

                    if (!allowedStatuses.Contains(inspectionStatus))
                        return BadRequest(
                            $"Invalid inspection status on receipt line {line.ReceiptLineId}.");
                }

                // Create inventory transaction
                var inventoryTransaction = new InventoryTransaction
                {
                    TransactionNumber = $"GR-{Guid.NewGuid():N}",
                    TransactionType = "RECEIPT",
                    TransactionDate = DateTime.UtcNow,
                    ReferenceNumber = receipt.ReceiptNumber,
                    Remarks = $"Goods receipt posting: {receipt.ReceiptNumber}",
                    Status = "POSTED",
                    CreatedBy = receipt.ReceivedBy,
                    PostedBy = receipt.ReceivedBy,
                    PostedAt = DateTime.UtcNow
                };

                _context.InventoryTransactions.Add(inventoryTransaction);

                await _context.SaveChangesAsync();

                // Process each receipt line
                foreach (var line in receipt.GoodsReceiptLines)
                {
                    string inspectionStatus =
                        line.InspectionStatus.Trim().ToUpper();

                    // Determine inventory stock status
                    string stockStatus =
                        inspectionStatus == "APPROVED"
                            ? "AVAILABLE"
                            : "QUARANTINE";

                    // Keep rejected goods quarantined rather than usable
                    if (inspectionStatus == "REJECTED")
                        stockStatus = "QUARANTINE";

                    // Update lot quality status
                    line.Lot.QualityStatus =
                        inspectionStatus == "APPROVED"
                            ? "APPROVED"
                            : inspectionStatus == "PENDING"
                                ? "PENDING"
                                : "QUARANTINED";

                    // Find existing inventory balance
                    var balance = await _context.InventoryBalances
                        .FirstOrDefaultAsync(b =>
                            b.LotId == line.LotId &&
                            b.BinId == line.ReceivingBinId.Value &&
                            b.StockStatus == stockStatus);

                    if (balance == null)
                    {
                        // Create new inventory balance
                        balance = new InventoryBalance
                        {
                            LotId = line.LotId,
                            BinId = line.ReceivingBinId.Value,
                            StockStatus = stockStatus,
                            QuantityOnHand = line.ReceivedQuantity,
                            QuantityReserved = 0,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.InventoryBalances.Add(balance);
                    }
                    else
                    {
                        // Add quantity to existing balance
                        balance.QuantityOnHand += line.ReceivedQuantity;
                        balance.UpdatedAt = DateTime.UtcNow;
                    }

                    // Create inventory transaction line
                    var transactionLine = new InventoryTransactionLine
                    {
                        TransactionId = inventoryTransaction.TransactionId,
                        LotId = line.LotId,
                        FromBinId = null,
                        ToBinId = line.ReceivingBinId.Value,
                        Quantity = line.ReceivedQuantity,
                        UomId = line.UomId,
                        LineRemarks = $"Goods receipt line {line.ReceiptLineId}"
                    };

                    _context.InventoryTransactionLines.Add(transactionLine);
                }

                // Mark receipt as posted
                receipt.Status = "POSTED";

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Goods receipt posted successfully.",
                    receiptId = receipt.ReceiptId,
                    receiptNumber = receipt.ReceiptNumber,
                    receiptStatus = receipt.Status,
                    inventoryTransactionId =
                        inventoryTransaction.TransactionId,
                    transactionNumber =
                        inventoryTransaction.TransactionNumber,
                    transactionType =
                        inventoryTransaction.TransactionType
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                var errorMessage = ex.InnerException?.Message ?? ex.Message;

                Console.WriteLine("GOODS RECEIPT POSTING ERROR:");
                Console.WriteLine(ex.ToString());

                return StatusCode(500, new
                {
                    message = "An error occurred while posting the goods receipt.",
                    error = errorMessage
                });
            }
        }


        // GET: api/goods-receipts/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var receipt = await _context.GoodsReceipts
                .Include(r => r.Warehouse)
                .Include(r => r.PurchaseOrder)
                .Include(r => r.GoodsReceiptLines)
                    .ThenInclude(l => l.Item)
                .Include(r => r.GoodsReceiptLines)
                    .ThenInclude(l => l.Lot)
                .FirstOrDefaultAsync(r => r.ReceiptId == id);

            if (receipt == null)
                return NotFound("Goods receipt not found.");

            return Ok(new
            {
                receipt.ReceiptId,
                receipt.ReceiptNumber,
                receipt.PurchaseOrderId,
                PoNumber = receipt.PurchaseOrder?.PoNumber,
                receipt.WarehouseId,
                WarehouseName = receipt.Warehouse.WarehouseName,
                receipt.ReceiptDate,
                receipt.DeliveryReference,
                receipt.Status,
                Lines = receipt.GoodsReceiptLines.Select(l => new
                {
                    l.ReceiptLineId,
                    l.ItemId,
                    ItemCode = l.Item.ItemCode,
                    ItemName = l.Item.ItemName,
                    l.LotId,
                    BatchCode = l.Lot.BatchCode,
                    l.ReceivedQuantity,
                    l.UomId,
                    l.InspectionStatus,
                    l.PoLineId,
                    l.ReceivingBinId
                })
            });
        }

        // POST: api/goods-receipts
        [HttpPost]
        public async Task<IActionResult> Create(
            CreateGoodsReceiptDto dto)
        {
            // Validate receipt number
            if (string.IsNullOrWhiteSpace(dto.ReceiptNumber))
                return BadRequest("Receipt number is required.");

            bool receiptExists = await _context.GoodsReceipts
                .AnyAsync(r => r.ReceiptNumber == dto.ReceiptNumber);

            if (receiptExists)
                return BadRequest("Receipt number already exists.");

            // Validate warehouse
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w =>
                    w.WarehouseId == dto.WarehouseId && w.IsActive);

            if (warehouse == null)
                return BadRequest("Warehouse does not exist or is inactive.");

            // Validate Purchase Order if supplied
            if (dto.PurchaseOrderId.HasValue)
            {
                bool poExists = await _context.PurchaseOrders
                    .AnyAsync(p =>
                        p.PurchaseOrderId == dto.PurchaseOrderId.Value);

                if (!poExists)
                    return BadRequest("Purchase Order does not exist.");
            }

            // Validate receipt lines
            if (dto.Lines == null || dto.Lines.Count == 0)
                return BadRequest("At least one receipt line is required.");

            foreach (var line in dto.Lines)
            {
                if (line.ReceivedQuantity <= 0)
                    return BadRequest("Received quantity must be greater than zero.");

                // Validate inspection status
                string inspectionStatus =
                    (line.InspectionStatus ?? "").Trim().ToUpper();

                string[] allowedStatuses =
                {
                    "PENDING",
                    "APPROVED",
                    "REJECTED",
                    "QUARANTINED"
                };

                if (!allowedStatuses.Contains(inspectionStatus))
                    return BadRequest(
                        $"Invalid inspection status: {line.InspectionStatus}");

                // Validate item
                var item = await _context.Items
                    .FirstOrDefaultAsync(i =>
                        i.ItemId == line.ItemId && i.IsActive);

                if (item == null)
                    return BadRequest($"Item ID {line.ItemId} does not exist or is inactive.");

                // Validate inventory lot belongs to the item
                bool lotExists = await _context.InventoryLots
                    .AnyAsync(l =>
                        l.LotId == line.LotId &&
                        l.ItemId == line.ItemId);

                if (!lotExists)
                    return BadRequest(
                        $"Inventory lot {line.LotId} does not belong to item {line.ItemId}.");

                // Validate UOM
                bool uomExists = await _context.UnitsOfMeasures
                    .AnyAsync(u =>
                        u.UomId == line.UomId && u.IsActive);

                if (!uomExists)
                    return BadRequest($"UOM ID {line.UomId} does not exist or is inactive.");

                // Validate PO line if supplied
                if (line.PoLineId.HasValue)
                {
                    if (!dto.PurchaseOrderId.HasValue)
                        return BadRequest(
                            "A Purchase Order must be selected when a PO line is supplied.");

                    bool poLineExists = await _context.PurchaseOrderLines
                        .AnyAsync(p =>
                            p.PoLineId == line.PoLineId.Value &&
                            p.PurchaseOrderId == dto.PurchaseOrderId.Value &&
                            p.ItemId == line.ItemId &&
                            p.UomId == line.UomId);

                    if (!poLineExists)
                        return BadRequest(
                            $"PO line {line.PoLineId} does not match the selected Purchase Order, item, or UOM.");
                }

                // Validate receiving bin if supplied
                if (line.ReceivingBinId.HasValue)
                {
                    var receivingBin = await _context.StorageBins
                        .Include(b => b.Zone)
                        .FirstOrDefaultAsync(b =>
                            b.BinId == line.ReceivingBinId.Value &&
                            b.IsActive);

                    if (receivingBin == null)
                    {
                        return BadRequest(
                            $"Receiving bin ID {line.ReceivingBinId} does not exist or is inactive.");
                    }

                    if (receivingBin.Zone.WarehouseId != dto.WarehouseId)
                    {
                        return BadRequest(
                            $"Receiving bin {receivingBin.BinCode} does not belong to the selected warehouse.");
                    }
                }
            }

            // Create receipt header
            var receipt = new GoodsReceipt
            {
                ReceiptNumber = dto.ReceiptNumber.Trim(),
                PurchaseOrderId = dto.PurchaseOrderId,
                WarehouseId = dto.WarehouseId,
                ReceiptDate = dto.ReceiptDate.HasValue
                ? DateTime.SpecifyKind(dto.ReceiptDate.Value, DateTimeKind.Utc)
                : DateTime.UtcNow,
                DeliveryReference = dto.DeliveryReference,
                ReceivedBy = dto.ReceivedBy,
                Status = "DRAFT"
            };

            // Create receipt lines
            foreach (var line in dto.Lines)
            {
                receipt.GoodsReceiptLines.Add(new GoodsReceiptLine
                {
                    ItemId = line.ItemId,
                    LotId = line.LotId,
                    ReceivedQuantity = line.ReceivedQuantity,
                    UomId = line.UomId,
                    ReceivingBinId = line.ReceivingBinId,
                    InspectionStatus =
                        line.InspectionStatus.Trim().ToUpper(),
                    PoLineId = line.PoLineId
                });
            }

            // Save header and lines together
            _context.GoodsReceipts.Add(receipt);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = receipt.ReceiptId },
                new
                {
                    message = "Goods receipt created successfully.",
                    receiptId = receipt.ReceiptId,
                    receiptNumber = receipt.ReceiptNumber,
                    status = receipt.Status
                });
        }
    }
}
