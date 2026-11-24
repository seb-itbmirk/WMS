using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.GoodsReceipts;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/goods-receipts")]
    [ApiController]
    [Authorize]
    public class GoodsReceiptsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public GoodsReceiptsController(WmsDbContext context)
        {
            _context = context;
        }

        // Gets the authenticated user's ID from the JWT.
        private long? GetCurrentUserId()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return long.TryParse(userId, out var id)
                ? id
                : null;
        }

        // Gets all warehouses assigned to the authenticated user.
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

        // Checks whether the user can access a specific warehouse.
        private async Task<bool> HasWarehouseAccessAsync(
            long warehouseId)
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

        // GET: api/goods-receipts
        [HttpGet]
        [Authorize(Policy = "Permission:receiving.view")]
        public async Task<IActionResult> GetAll()
        {
            var warehouseIds =
                await GetAccessibleWarehouseIdsAsync();

            var receipts = await _context.GoodsReceipts
                .AsNoTracking()
                .Where(r => warehouseIds.Contains(r.WarehouseId))
                .OrderByDescending(r => r.ReceiptDate)
                .Select(r => new
                {
                    r.ReceiptId,
                    r.ReceiptNumber,
                    r.PurchaseOrderId,
                    PoNumber = r.PurchaseOrder != null
                        ? r.PurchaseOrder.PoNumber
                        : null,
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
                        UomCode = l.Uom.UomCode,
                        l.InspectionStatus,
                        l.PoLineId,
                        l.ReceivingBinId
                    }).ToList()
                })
                .ToListAsync();

            return Ok(receipts);
        }

        // GET: api/goods-receipts/1
        [HttpGet("{id:long}")]
        [Authorize(Policy = "Permission:receiving.view")]
        public async Task<IActionResult> GetById(long id)
        {
            var warehouseIds =
                await GetAccessibleWarehouseIdsAsync();

            var receipt = await _context.GoodsReceipts
                .AsNoTracking()
                .Where(r =>
                    r.ReceiptId == id &&
                    warehouseIds.Contains(r.WarehouseId))
                .Select(r => new
                {
                    r.ReceiptId,
                    r.ReceiptNumber,
                    r.PurchaseOrderId,
                    PoNumber = r.PurchaseOrder != null
                        ? r.PurchaseOrder.PoNumber
                        : null,
                    r.WarehouseId,
                    WarehouseName = r.Warehouse.WarehouseName,
                    r.ReceiptDate,
                    r.DeliveryReference,
                    r.Status,
                    r.ReceivedBy,
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
                        UomCode = l.Uom.UomCode,
                        l.InspectionStatus,
                        l.PoLineId,
                        l.ReceivingBinId
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (receipt == null)
            {
                return NotFound(new
                {
                    message = "Goods receipt not found."
                });
            }

            return Ok(receipt);
        }

        // POST: api/goods-receipts
        [HttpPost]
        [Authorize(Policy = "Permission:receiving.manage")]
        public async Task<IActionResult> Create(
            [FromBody] CreateGoodsReceiptDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Goods receipt details are required."
                });
            }

            var currentUserId = GetCurrentUserId();

            if (!currentUserId.HasValue)
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the authenticated user."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.ReceiptNumber))
            {
                return BadRequest(new
                {
                    message = "Receipt number is required."
                });
            }

            if (dto.WarehouseId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid warehouse is required."
                });
            }

            if (dto.Lines == null || dto.Lines.Count == 0)
            {
                return BadRequest(new
                {
                    message = "At least one receipt line is required."
                });
            }

            // Enforce warehouse access before creating the receipt.
            var hasWarehouseAccess =
                await HasWarehouseAccessAsync(dto.WarehouseId);

            if (!hasWarehouseAccess)
            {
                return NotFound(new
                {
                    message = "Warehouse not found."
                });
            }

            var receiptNumber = dto.ReceiptNumber.Trim();

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var receiptExists = await _context.GoodsReceipts
                    .AnyAsync(r => r.ReceiptNumber == receiptNumber);

                if (receiptExists)
                {
                    return Conflict(new
                    {
                        message = "Receipt number already exists."
                    });
                }

                var warehouse = await _context.Warehouses
                    .FirstOrDefaultAsync(w =>
                        w.WarehouseId == dto.WarehouseId &&
                        w.IsActive);

                if (warehouse == null)
                {
                    return BadRequest(new
                    {
                        message = "Warehouse does not exist or is inactive."
                    });
                }

                if (dto.PurchaseOrderId.HasValue)
                {
                    var purchaseOrderExists =
                        await _context.PurchaseOrders.AnyAsync(p =>
                            p.PurchaseOrderId == dto.PurchaseOrderId.Value);

                    if (!purchaseOrderExists)
                    {
                        return BadRequest(new
                        {
                            message = "Purchase order does not exist."
                        });
                    }
                }

                foreach (var line in dto.Lines)
                {
                    if (line.ItemId <= 0 ||
                        line.LotId <= 0 ||
                        line.UomId <= 0 ||
                        line.ReceivedQuantity <= 0)
                    {
                        return BadRequest(new
                        {
                            message = "Every receipt line must have valid item, lot, UOM, and positive received quantity."
                        });
                    }

                    var inspectionStatus =
                        (line.InspectionStatus ?? string.Empty)
                        .Trim()
                        .ToUpperInvariant();

                    string[] allowedStatuses =
                    {
                        "PENDING",
                        "APPROVED",
                        "REJECTED",
                        "QUARANTINED"
                    };

                    if (!allowedStatuses.Contains(inspectionStatus))
                    {
                        return BadRequest(new
                        {
                            message = $"Invalid inspection status: {line.InspectionStatus}"
                        });
                    }

                    var item = await _context.Items
                        .FirstOrDefaultAsync(i =>
                            i.ItemId == line.ItemId &&
                            i.IsActive);

                    if (item == null)
                    {
                        return BadRequest(new
                        {
                            message = $"Item ID {line.ItemId} does not exist or is inactive."
                        });
                    }

                    if (line.UomId != item.BaseUomId)
                    {
                        return BadRequest(new
                        {
                            message = $"Receipt line for item {item.ItemCode} must use its base UOM."
                        });
                    }

                    var lot = await _context.InventoryLots
                        .FirstOrDefaultAsync(l =>
                            l.LotId == line.LotId &&
                            l.ItemId == line.ItemId);

                    if (lot == null)
                    {
                        return BadRequest(new
                        {
                            message = $"Inventory lot {line.LotId} does not belong to item {line.ItemId}."
                        });
                    }

                    var uomExists = await _context.UnitsOfMeasures
                        .AnyAsync(u =>
                            u.UomId == line.UomId &&
                            u.IsActive);

                    if (!uomExists)
                    {
                        return BadRequest(new
                        {
                            message = $"UOM ID {line.UomId} does not exist or is inactive."
                        });
                    }

                    if (line.PoLineId.HasValue)
                    {
                        if (!dto.PurchaseOrderId.HasValue)
                        {
                            return BadRequest(new
                            {
                                message = "A purchase order must be selected when a PO line is supplied."
                            });
                        }

                        var poLine = await _context.PurchaseOrderLines
                            .FirstOrDefaultAsync(p =>
                                p.PoLineId == line.PoLineId.Value &&
                                p.PurchaseOrderId ==
                                    dto.PurchaseOrderId.Value &&
                                p.ItemId == line.ItemId &&
                                p.UomId == line.UomId);

                        if (poLine == null)
                        {
                            return BadRequest(new
                            {
                                message = $"PO line {line.PoLineId} does not match the selected purchase order, item, or UOM."
                            });
                        }
                    }

                    if (line.ReceivingBinId.HasValue)
                    {
                        var receivingBin = await _context.StorageBins
                            .Include(b => b.Zone)
                            .FirstOrDefaultAsync(b =>
                                b.BinId == line.ReceivingBinId.Value &&
                                b.IsActive);

                        if (receivingBin == null)
                        {
                            return BadRequest(new
                            {
                                message = $"Receiving bin {line.ReceivingBinId} does not exist or is inactive."
                            });
                        }

                        if (receivingBin.Zone.WarehouseId !=
                            dto.WarehouseId)
                        {
                            return BadRequest(new
                            {
                                message = $"Receiving bin {receivingBin.BinCode} does not belong to the selected warehouse."
                            });
                        }
                    }
                }

                var receipt = new GoodsReceipt
                {
                    ReceiptNumber = receiptNumber,
                    PurchaseOrderId = dto.PurchaseOrderId,
                    WarehouseId = dto.WarehouseId,
                    ReceiptDate = dto.ReceiptDate.HasValue
                        ? DateTime.SpecifyKind(
                            dto.ReceiptDate.Value,
                            DateTimeKind.Utc)
                        : DateTime.UtcNow,
                    DeliveryReference = dto.DeliveryReference,

                    // Never accept ReceivedBy from the request.
                    ReceivedBy = currentUserId.Value,

                    Status = "DRAFT"
                };

                foreach (var line in dto.Lines)
                {
                    receipt.GoodsReceiptLines.Add(
                        new GoodsReceiptLine
                        {
                            ItemId = line.ItemId,
                            LotId = line.LotId,
                            ReceivedQuantity = line.ReceivedQuantity,
                            UomId = line.UomId,
                            ReceivingBinId = line.ReceivingBinId,
                            InspectionStatus =
                                line.InspectionStatus.Trim()
                                    .ToUpperInvariant(),
                            PoLineId = line.PoLineId
                        });
                }

                _context.GoodsReceipts.Add(receipt);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = receipt.ReceiptId },
                    new
                    {
                        message = "Goods receipt created successfully.",
                        receiptId = receipt.ReceiptId,
                        receiptNumber = receipt.ReceiptNumber,
                        receipt.Status,
                        lineCount = receipt.GoodsReceiptLines.Count
                    });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // POST: api/goods-receipts/1/post
        [HttpPost("{id:long}/post")]
        [Authorize(Policy = "Permission:receiving.manage")]
        public async Task<IActionResult> Post(long id)
        {
            var currentUserId = GetCurrentUserId();

            if (!currentUserId.HasValue)
            {
                return Unauthorized(new
                {
                    message = "Unable to identify the authenticated user."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var receipt = await _context.GoodsReceipts
                    .Include(r => r.GoodsReceiptLines)
                        .ThenInclude(l => l.Item)
                    .Include(r => r.GoodsReceiptLines)
                        .ThenInclude(l => l.Lot)
                    .FirstOrDefaultAsync(r => r.ReceiptId == id);

                if (receipt == null)
                {
                    return NotFound(new
                    {
                        message = "Goods receipt not found."
                    });
                }

                // Enforce warehouse access before posting.
                var hasWarehouseAccess =
                    await HasWarehouseAccessAsync(receipt.WarehouseId);

                if (!hasWarehouseAccess)
                {
                    return NotFound(new
                    {
                        message = "Goods receipt not found."
                    });
                }

                if (receipt.Status != "DRAFT")
                {
                    return BadRequest(new
                    {
                        message = "Only DRAFT goods receipts can be posted."
                    });
                }

                if (receipt.GoodsReceiptLines.Count == 0)
                {
                    return BadRequest(new
                    {
                        message = "Goods receipt has no receipt lines."
                    });
                }

                var warehouseExists = await _context.Warehouses
                    .AnyAsync(w =>
                        w.WarehouseId == receipt.WarehouseId &&
                        w.IsActive);

                if (!warehouseExists)
                {
                    return BadRequest(new
                    {
                        message = "The receipt warehouse does not exist or is inactive."
                    });
                }

                // Validate all receipt lines before changing inventory.
                foreach (var line in receipt.GoodsReceiptLines)
                {
                    if (line.ReceivedQuantity <= 0)
                    {
                        return BadRequest(new
                        {
                            message = $"Invalid received quantity for receipt line {line.ReceiptLineId}."
                        });
                    }

                    if (!line.ReceivingBinId.HasValue)
                    {
                        return BadRequest(new
                        {
                            message = $"Receipt line {line.ReceiptLineId} has no receiving bin."
                        });
                    }

                    if (line.Item == null || !line.Item.IsActive)
                    {
                        return BadRequest(new
                        {
                            message = $"Item ID {line.ItemId} does not exist or is inactive."
                        });
                    }

                    if (line.Lot == null ||
                        line.Lot.ItemId != line.ItemId)
                    {
                        return BadRequest(new
                        {
                            message = $"Inventory lot {line.LotId} does not belong to item {line.ItemId}."
                        });
                    }

                    if (line.UomId != line.Item.BaseUomId)
                    {
                        return BadRequest(new
                        {
                            message = $"Receipt line {line.ReceiptLineId} must use the item's base UOM."
                        });
                    }

                    var receivingBin = await _context.StorageBins
                        .Include(b => b.Zone)
                        .FirstOrDefaultAsync(b =>
                            b.BinId == line.ReceivingBinId.Value &&
                            b.IsActive);

                    if (receivingBin == null)
                    {
                        return BadRequest(new
                        {
                            message = $"Receiving bin {line.ReceivingBinId} does not exist or is inactive."
                        });
                    }

                    if (receivingBin.Zone.WarehouseId !=
                        receipt.WarehouseId)
                    {
                        return BadRequest(new
                        {
                            message = $"Receiving bin {receivingBin.BinCode} does not belong to the receipt warehouse."
                        });
                    }

                    var inspectionStatus =
                        (line.InspectionStatus ?? string.Empty)
                        .Trim()
                        .ToUpperInvariant();

                    string[] allowedStatuses =
                    {
                        "PENDING",
                        "APPROVED",
                        "REJECTED",
                        "QUARANTINED"
                    };

                    if (!allowedStatuses.Contains(inspectionStatus))
                    {
                        return BadRequest(new
                        {
                            message = $"Invalid inspection status on receipt line {line.ReceiptLineId}."
                        });
                    }

                    if (line.PoLineId.HasValue)
                    {
                        if (!receipt.PurchaseOrderId.HasValue)
                        {
                            return BadRequest(new
                            {
                                message = "A purchase order is required for receipt lines linked to a PO line."
                            });
                        }

                        var poLine = await _context.PurchaseOrderLines
                            .FirstOrDefaultAsync(p =>
                                p.PoLineId == line.PoLineId.Value &&
                                p.PurchaseOrderId ==
                                    receipt.PurchaseOrderId.Value &&
                                p.ItemId == line.ItemId &&
                                p.UomId == line.UomId);

                        if (poLine == null)
                        {
                            return BadRequest(new
                            {
                                message = $"PO line {line.PoLineId} no longer matches the receipt."
                            });
                        }

                        var previouslyReceived =
                            await _context.GoodsReceiptLines
                                .Where(l =>
                                    l.PoLineId == poLine.PoLineId &&
                                    l.Receipt.Status == "POSTED")
                                .SumAsync(l =>
                                    (decimal?)l.ReceivedQuantity) ?? 0m;

                        var currentReceiptQuantity =
                            receipt.GoodsReceiptLines
                                .Where(l =>
                                    l.PoLineId == poLine.PoLineId)
                                .Sum(l => l.ReceivedQuantity);

                        if (previouslyReceived +
                            currentReceiptQuantity >
                            poLine.OrderedQuantity)
                        {
                            return BadRequest(new
                            {
                                message = $"Receipt quantity exceeds the remaining ordered quantity for PO line {poLine.PoLineId}.",
                                orderedQuantity = poLine.OrderedQuantity,
                                previouslyReceived,
                                currentReceiptQuantity,
                                remainingQuantity =
                                    poLine.OrderedQuantity -
                                    previouslyReceived
                            });
                        }
                    }
                }

                var inventoryTransaction = new InventoryTransaction
                {
                    TransactionNumber = $"GR-{Guid.NewGuid():N}",
                    TransactionType = "RECEIPT",
                    TransactionDate = DateTime.UtcNow,
                    ReferenceNumber = receipt.ReceiptNumber,
                    Remarks = $"Goods receipt posting: {receipt.ReceiptNumber}",
                    Status = "POSTED",

                    // Use the authenticated user for audit fields.
                    CreatedBy = currentUserId.Value,
                    PostedBy = currentUserId.Value,
                    PostedAt = DateTime.UtcNow
                };

                _context.InventoryTransactions.Add(inventoryTransaction);

                await _context.SaveChangesAsync();

                foreach (var line in receipt.GoodsReceiptLines)
                {
                    var inspectionStatus =
                        line.InspectionStatus.Trim()
                            .ToUpperInvariant();

                    var stockStatus =
                        inspectionStatus == "APPROVED"
                            ? "AVAILABLE"
                            : "QUARANTINE";

                    line.Lot.QualityStatus =
                        inspectionStatus == "APPROVED"
                            ? "APPROVED"
                            : inspectionStatus == "PENDING"
                                ? "PENDING"
                                : "QUARANTINED";

                    var balance = await _context.InventoryBalances
                        .FirstOrDefaultAsync(b =>
                            b.LotId == line.LotId &&
                            b.BinId == line.ReceivingBinId!.Value &&
                            b.StockStatus == stockStatus);

                    if (balance == null)
                    {
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
                        balance.QuantityOnHand += line.ReceivedQuantity;
                        balance.UpdatedAt = DateTime.UtcNow;
                    }

                    var transactionLine =
                        new InventoryTransactionLine
                        {
                            TransactionId =
                                inventoryTransaction.TransactionId,
                            LotId = line.LotId,
                            FromBinId = null,
                            ToBinId = line.ReceivingBinId.Value,
                            Quantity = line.ReceivedQuantity,
                            UomId = line.UomId,
                            LineRemarks =
                                $"Goods receipt line {line.ReceiptLineId}"
                        };

                    _context.InventoryTransactionLines.Add(
                        transactionLine);
                }

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

                var errorMessage =
                    ex.InnerException?.Message ?? ex.Message;

                return StatusCode(500, new
                {
                    message = "An error occurred while posting the goods receipt.",
                    error = errorMessage
                });
            }
        }
    }
}