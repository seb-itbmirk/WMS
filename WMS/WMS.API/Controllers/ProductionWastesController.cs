
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WMS.API.Data;
using WMS.API.DTOs.ProductionWaste;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/production-wastes")]
    [ApiController]
    public class ProductionWastesController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public ProductionWastesController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/production-wastes
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var wastes = await _context.ProductionWastes
                .OrderByDescending(w => w.RecordedAt)
                .Select(w => new
                {
                    w.WasteId,
                    w.ProductionOrderId,
                    w.ProductionOrder.ProductionNumber,
                    w.LotId,
                    w.Lot.BatchCode,
                    w.Lot.ItemId,
                    ItemCode = w.Lot.Item.ItemCode,
                    ItemName = w.Lot.Item.ItemName,
                    w.WastedQuantity,
                    w.UomId,
                    UomCode = w.Uom.UomCode,
                    w.WasteReason,
                    w.RecordedAt,
                    w.RecordedBy
                })
                .ToListAsync();

            return Ok(wastes);
        }

        // GET: api/production-wastes/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var waste = await _context.ProductionWastes
                .Where(w => w.WasteId == id)
                .Select(w => new
                {
                    w.WasteId,
                    w.ProductionOrderId,
                    w.ProductionOrder.ProductionNumber,
                    w.LotId,
                    w.Lot.BatchCode,
                    w.Lot.ItemId,
                    ItemCode = w.Lot.Item.ItemCode,
                    ItemName = w.Lot.Item.ItemName,
                    w.WastedQuantity,
                    w.UomId,
                    UomCode = w.Uom.UomCode,
                    w.WasteReason,
                    w.RecordedAt,
                    w.RecordedBy
                })
                .FirstOrDefaultAsync();

            if (waste == null)
                return NotFound(new
                {
                    message = "Production waste record not found."
                });

            return Ok(waste);
        }

        // GET: api/production-wastes/production-order/1
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

            var wastes = await _context.ProductionWastes
                .Where(w => w.ProductionOrderId == productionOrderId)
                .OrderByDescending(w => w.RecordedAt)
                .Select(w => new
                {
                    w.WasteId,
                    w.ProductionOrderId,
                    w.LotId,
                    w.Lot.BatchCode,
                    w.Lot.ItemId,
                    ItemCode = w.Lot.Item.ItemCode,
                    ItemName = w.Lot.Item.ItemName,
                    w.WastedQuantity,
                    w.UomId,
                    UomCode = w.Uom.UomCode,
                    w.WasteReason,
                    w.RecordedAt,
                    w.RecordedBy
                })
                .ToListAsync();

            return Ok(wastes);
        }

        // POST: api/production-wastes
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductionWasteDto dto)
        {
            if (dto.ProductionOrderId <= 0 ||
                dto.LotId <= 0 ||
                dto.UomId <= 0 ||
                dto.WastedQuantity <= 0)
            {
                return BadRequest(new
                {
                    message = "Enter valid production order, lot, UOM, and waste quantity."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.WasteReason))
            {
                return BadRequest(new
                {
                    message = "Waste reason is required."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var order = await _context.ProductionOrders
                    .Include(p => p.ProductionMaterialRequirements)
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

                var lot = await _context.InventoryLots
                    .FirstOrDefaultAsync(l => l.LotId == dto.LotId);

                if (lot == null)
                    return NotFound(new
                    {
                        message = "Inventory lot not found."
                    });

                var requirement = order.ProductionMaterialRequirements
                    .FirstOrDefault(r => r.ItemId == lot.ItemId);

                if (requirement == null)
                    return BadRequest(new
                    {
                        message = "This lot's item is not part of the production order requirements."
                    });

                if (dto.UomId != requirement.UomId)
                {
                    return BadRequest(new
                    {
                        message = "Waste UOM must match the production material requirement UOM."
                    });
                }

                // Get total quantity issued for this lot
                // from POSTED production material issues.
                var issuedQuantity =
                    await _context.ProductionMaterialIssues
                        .Where(i =>
                            i.ProductionOrderId == dto.ProductionOrderId &&
                            i.Status == "POSTED")
                        .SelectMany(i => i.ProductionMaterialIssueLines)
                        .Where(l => l.LotId == dto.LotId)
                        .SumAsync(l => (decimal?)l.IssuedQuantity) ?? 0;

                // Get total consumption recorded for this lot.
                var consumedQuantity =
                    await _context.ProductionConsumptions
                        .Where(c =>
                            c.ProductionOrderId == dto.ProductionOrderId &&
                            c.LotId == dto.LotId)
                        .SumAsync(c => (decimal?)c.ConsumedQuantity) ?? 0;

                // Get previously recorded waste for this lot.
                var previousWaste =
                    await _context.ProductionWastes
                        .Where(w =>
                            w.ProductionOrderId == dto.ProductionOrderId &&
                            w.LotId == dto.LotId)
                        .SumAsync(w => (decimal?)w.WastedQuantity) ?? 0;

                var remainingLotQuantity =
                    issuedQuantity - consumedQuantity - previousWaste;

                if (dto.WastedQuantity > remainingLotQuantity)
                {
                    return BadRequest(new
                    {
                        message = "Waste quantity exceeds the remaining issued quantity for this lot.",
                        issuedQuantity,
                        consumedQuantity,
                        previousWaste,
                        remainingQuantity = remainingLotQuantity,
                        requestedWasteQuantity = dto.WastedQuantity
                    });
                }

                // Validate against the total issued quantity
                // for this material requirement across all lots.
                var totalConsumed =
                    await _context.ProductionConsumptions
                        .Where(c =>
                            c.ProductionOrderId == dto.ProductionOrderId &&
                            c.Lot.ItemId == lot.ItemId)
                        .SumAsync(c => (decimal?)c.ConsumedQuantity) ?? 0;

                var totalWaste =
                    await _context.ProductionWastes
                        .Where(w =>
                            w.ProductionOrderId == dto.ProductionOrderId &&
                            w.Lot.ItemId == lot.ItemId)
                        .SumAsync(w => (decimal?)w.WastedQuantity) ?? 0;

                var totalIssued =
                    requirement.IssuedQuantity;

                if (totalConsumed + totalWaste + dto.WastedQuantity >
                    totalIssued)
                {
                    return BadRequest(new
                    {
                        message = "Total consumption and waste cannot exceed the issued quantity for this material.",
                        issuedQuantity = totalIssued,
                        consumedQuantity = totalConsumed,
                        previousWaste = totalWaste,
                        requestedWasteQuantity = dto.WastedQuantity
                    });
                }

                var waste = new ProductionWaste
                {
                    ProductionOrderId = dto.ProductionOrderId,
                    LotId = dto.LotId,
                    WastedQuantity = dto.WastedQuantity,
                    UomId = dto.UomId,
                    WasteReason = dto.WasteReason.Trim(),
                    RecordedAt = dto.RecordedAt ?? DateTime.UtcNow,
                    RecordedBy = dto.RecordedBy
                };

                _context.ProductionWastes.Add(waste);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = waste.WasteId },
                    new
                    {
                        message = "Production waste recorded successfully.",
                        wasteId = waste.WasteId,
                        productionOrderId = order.ProductionOrderId,
                        productionNumber = order.ProductionNumber,
                        lotId = lot.LotId,
                        batchCode = lot.BatchCode,
                        itemId = lot.ItemId,
                        wastedQuantity = waste.WastedQuantity,
                        uomId = waste.UomId,
                        wasteReason = waste.WasteReason,
                        recordedAt = waste.RecordedAt,
                        issuedQuantity = totalIssued,
                        totalConsumed,
                        totalWaste = totalWaste + waste.WastedQuantity,
                        remainingQuantity =
                            totalIssued - totalConsumed -
                            totalWaste - waste.WastedQuantity
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
