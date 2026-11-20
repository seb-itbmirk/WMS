
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.ProductionOutputs;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/production-outputs")]
    [ApiController]
    public class ProductionOutputsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public ProductionOutputsController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/production-outputs
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var outputs = await _context.ProductionOutputs
                .OrderByDescending(o => o.ProductionDate)
                .Select(o => new
                {
                    o.OutputId,
                    o.ProductionOrderId,
                    o.ProductionOrder.ProductionNumber,
                    o.FinishedLotId,
                    o.FinishedLot.BatchCode,
                    o.FinishedLot.ItemId,
                    ItemCode = o.FinishedLot.Item.ItemCode,
                    ItemName = o.FinishedLot.Item.ItemName,
                    o.ProducedQuantity,
                    o.UomId,
                    UomCode = o.Uom.UomCode,
                    o.ProductionDate,
                    o.QualityStatus
                })
                .ToListAsync();

            return Ok(outputs);
        }

        // GET: api/production-outputs/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var output = await _context.ProductionOutputs
                .Where(o => o.OutputId == id)
                .Select(o => new
                {
                    o.OutputId,
                    o.ProductionOrderId,
                    o.ProductionOrder.ProductionNumber,
                    o.FinishedLotId,
                    o.FinishedLot.BatchCode,
                    o.FinishedLot.ItemId,
                    ItemCode = o.FinishedLot.Item.ItemCode,
                    ItemName = o.FinishedLot.Item.ItemName,
                    o.ProducedQuantity,
                    o.UomId,
                    UomCode = o.Uom.UomCode,
                    o.ProductionDate,
                    o.QualityStatus
                })
                .FirstOrDefaultAsync();

            if (output == null)
                return NotFound(new
                {
                    message = "Production output not found."
                });

            return Ok(output);
        }

        // GET: api/production-outputs/production-order/1
        [HttpGet("production-order/{productionOrderId:long}")]
        public async Task<IActionResult> GetByProductionOrder(
            long productionOrderId)
        {
            var orderExists = await _context.ProductionOrders
                .AnyAsync(p => p.ProductionOrderId == productionOrderId);

            if (!orderExists)
                return NotFound(new
                {
                    message = "Production order not found."
                });

            var outputs = await _context.ProductionOutputs
                .Where(o => o.ProductionOrderId == productionOrderId)
                .OrderByDescending(o => o.ProductionDate)
                .Select(o => new
                {
                    o.OutputId,
                    o.ProductionOrderId,
                    o.FinishedLotId,
                    o.FinishedLot.BatchCode,
                    o.FinishedLot.ItemId,
                    ItemCode = o.FinishedLot.Item.ItemCode,
                    ItemName = o.FinishedLot.Item.ItemName,
                    o.ProducedQuantity,
                    o.UomId,
                    UomCode = o.Uom.UomCode,
                    o.ProductionDate,
                    o.QualityStatus
                })
                .ToListAsync();

            return Ok(outputs);
        }

        // POST: api/production-outputs
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductionOutputDto dto)
        {
            if (dto.ProductionOrderId <= 0 ||
                dto.ProducedQuantity <= 0 ||
                dto.UomId <= 0 ||
                dto.DestinationBinId <= 0)
            {
                return BadRequest(new
                {
                    message = "Enter valid production order, quantity, UOM, and destination bin."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.BatchCode))
            {
                return BadRequest(new
                {
                    message = "Finished-goods batch code is required."
                });
            }

            if (dto.ManufactureDate.HasValue &&
                dto.ExpiryDate.HasValue &&
                dto.ExpiryDate < dto.ManufactureDate)
            {
                return BadRequest(new
                {
                    message = "Expiry date cannot be earlier than manufacture date."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var order = await _context.ProductionOrders
                    .FirstOrDefaultAsync(p =>
                        p.ProductionOrderId == dto.ProductionOrderId);

                if (order == null)
                    return NotFound(new
                    {
                        message = "Production order not found."
                    });

                if (order.ProductionStatus != "RELEASED" &&
                    order.ProductionStatus != "IN_PROGRESS")
                {
                    return BadRequest(new
                    {
                        message = "Production order must be RELEASED or IN_PROGRESS."
                    });
                }

                var finishedItem = await _context.Items
                    .FirstOrDefaultAsync(i =>
                        i.ItemId == order.FinishedItemId);

                if (finishedItem == null)
                    return NotFound(new
                    {
                        message = "Finished product not found."
                    });

                if (!finishedItem.IsActive)
                    return BadRequest(new
                    {
                        message = "Finished product is inactive."
                    });

                if (dto.UomId != order.UomId)
                    return BadRequest(new
                    {
                        message = "Output UOM must match the production order UOM."
                    });

                var bin = await _context.StorageBins
                    .Include(b => b.Zone)
                    .FirstOrDefaultAsync(b =>
                        b.BinId == dto.DestinationBinId);

                if (bin == null)
                    return NotFound(new
                    {
                        message = "Destination bin not found."
                    });

                if (!bin.IsActive)
                    return BadRequest(new
                    {
                        message = "Destination bin is inactive."
                    });

                if (bin.Zone.WarehouseId != order.WarehouseId)
                    return BadRequest(new
                    {
                        message = "Destination bin must belong to the production order warehouse."
                    });

                var uomExists = await _context.UnitsOfMeasures
                    .AnyAsync(u => u.UomId == dto.UomId);

                if (!uomExists)
                    return BadRequest(new
                    {
                        message = "Unit of measure not found."
                    });

                var batchCode = dto.BatchCode.Trim();

                var batchExists = await _context.InventoryLots
                    .AnyAsync(l => l.BatchCode == batchCode);

                if (batchExists)
                    return BadRequest(new
                    {
                        message = "Batch code already exists."
                    });

                // Create finished-goods lot.
                var finishedLot = new InventoryLot
                {
                    ItemId = order.FinishedItemId,
                    BatchCode = batchCode,
                    ManufactureDate = dto.ManufactureDate ??
                                      DateOnly.FromDateTime(DateTime.UtcNow),
                    ExpiryDate = dto.ExpiryDate,
                    ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    QualityStatus = "PENDING",
                    Notes = "Created from production output.",
                    CreatedAt = DateTime.UtcNow
                };

                _context.InventoryLots.Add(finishedLot);
                await _context.SaveChangesAsync();

                // Create production output record.
                var output = new ProductionOutput
                {
                    ProductionOrderId = order.ProductionOrderId,
                    FinishedLotId = finishedLot.LotId,
                    ProducedQuantity = dto.ProducedQuantity,
                    UomId = dto.UomId,
                    ProductionDate = dto.ProductionDate ?? DateTime.UtcNow,
                    QualityStatus = "PENDING"
                };

                _context.ProductionOutputs.Add(output);

                // Create inventory transaction.
                var inventoryTransaction = new InventoryTransaction
                {
                    TransactionNumber =
                        $"PROD-OUTPUT-{Guid.NewGuid():N}",
                    TransactionType = "PRODUCTION_OUTPUT",
                    TransactionDate = DateTime.UtcNow,
                    ReferenceNumber = order.ProductionNumber,
                    Remarks = $"Production output for {order.ProductionNumber}",
                    Status = "POSTED",
                    CreatedBy = dto.RecordedBy,
                    PostedBy = dto.RecordedBy,
                    PostedAt = DateTime.UtcNow
                };

                _context.InventoryTransactions.Add(inventoryTransaction);
                await _context.SaveChangesAsync();

                // Create inventory transaction line.
                var transactionLine = new InventoryTransactionLine
                {
                    TransactionId = inventoryTransaction.TransactionId,
                    LotId = finishedLot.LotId,
                    FromBinId = null,
                    ToBinId = bin.BinId,
                    Quantity = dto.ProducedQuantity,
                    UomId = dto.UomId,
                    LineRemarks = "Finished goods production output."
                };

                _context.InventoryTransactionLines.Add(transactionLine);

                // Create or update inventory balance.
                var balance = await _context.InventoryBalances
                    .FirstOrDefaultAsync(b =>
                        b.LotId == finishedLot.LotId &&
                        b.BinId == bin.BinId &&
                        b.StockStatus == "QUARANTINE");

                if (balance == null)
                {
                    balance = new InventoryBalance
                    {
                        LotId = finishedLot.LotId,
                        BinId = bin.BinId,
                        StockStatus = "QUARANTINE",
                        QuantityOnHand = dto.ProducedQuantity,
                        QuantityReserved = 0,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.InventoryBalances.Add(balance);
                }
                else
                {
                    balance.QuantityOnHand += dto.ProducedQuantity;
                    balance.UpdatedAt = DateTime.UtcNow;
                }

                // Update production order status.
                order.ProductionStatus = "IN_PROGRESS";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = output.OutputId },
                    new
                    {
                        message = "Production output recorded successfully.",
                        outputId = output.OutputId,
                        productionOrderId = order.ProductionOrderId,
                        productionNumber = order.ProductionNumber,
                        finishedLotId = finishedLot.LotId,
                        batchCode = finishedLot.BatchCode,
                        finishedItemId = finishedLot.ItemId,
                        producedQuantity = output.ProducedQuantity,
                        uomId = output.UomId,
                        destinationBinId = bin.BinId,
                        inventoryTransactionId =
                            inventoryTransaction.TransactionId,
                        inventoryTransactionNumber =
                            inventoryTransaction.TransactionNumber,
                        qualityStatus = finishedLot.QualityStatus,
                        stockStatus = balance.StockStatus
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
