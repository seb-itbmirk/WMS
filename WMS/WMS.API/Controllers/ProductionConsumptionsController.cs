using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.ProductionConsumptions;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/production-consumptions")]
    [ApiController]
    public class ProductionConsumptionsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public ProductionConsumptionsController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/production-consumptions
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var consumptions = await _context.ProductionConsumptions
                .OrderByDescending(c => c.ConsumptionDate)
                .Select(c => new
                {
                    c.ConsumptionId,
                    c.ProductionOrderId,
                    c.ProductionOrder.ProductionNumber,
                    c.LotId,
                    c.Lot.BatchCode,
                    c.Lot.ItemId,
                    ItemCode = c.Lot.Item.ItemCode,
                    ItemName = c.Lot.Item.ItemName,
                    c.ConsumedQuantity,
                    c.UomId,
                    UomCode = c.Uom.UomCode,
                    c.ConsumptionDate,
                    c.RecordedBy
                })
                .ToListAsync();

            return Ok(consumptions);
        }

        // GET: api/production-consumptions/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var consumption = await _context.ProductionConsumptions
                .Where(c => c.ConsumptionId == id)
                .Select(c => new
                {
                    c.ConsumptionId,
                    c.ProductionOrderId,
                    c.ProductionOrder.ProductionNumber,
                    c.LotId,
                    c.Lot.BatchCode,
                    c.Lot.ItemId,
                    ItemCode = c.Lot.Item.ItemCode,
                    ItemName = c.Lot.Item.ItemName,
                    c.ConsumedQuantity,
                    c.UomId,
                    UomCode = c.Uom.UomCode,
                    c.ConsumptionDate,
                    c.RecordedBy
                })
                .FirstOrDefaultAsync();

            if (consumption == null)
                return NotFound(new
                {
                    message = "Production consumption record not found."
                });

            return Ok(consumption);
        }

        // GET: api/production-consumptions/production-order/1
        [HttpGet("production-order/{productionOrderId:long}")]
        public async Task<IActionResult> GetByProductionOrder(long productionOrderId)
        {
            var orderExists = await _context.ProductionOrders
                .AnyAsync(p => p.ProductionOrderId == productionOrderId);

            if (!orderExists)
                return NotFound(new
                {
                    message = "Production order not found."
                });

            var consumptions = await _context.ProductionConsumptions
                .Where(c => c.ProductionOrderId == productionOrderId)
                .OrderByDescending(c => c.ConsumptionDate)
                .Select(c => new
                {
                    c.ConsumptionId,
                    c.ProductionOrderId,
                    c.LotId,
                    c.Lot.BatchCode,
                    c.Lot.ItemId,
                    ItemCode = c.Lot.Item.ItemCode,
                    ItemName = c.Lot.Item.ItemName,
                    c.ConsumedQuantity,
                    c.UomId,
                    UomCode = c.Uom.UomCode,
                    c.ConsumptionDate,
                    c.RecordedBy
                })
                .ToListAsync();

            return Ok(consumptions);
        }

        // POST: api/production-consumptions
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductionConsumptionDto dto)
        {
            if (dto.ProductionOrderId <= 0 ||
                dto.LotId <= 0 ||
                dto.UomId <= 0 ||
                dto.ConsumedQuantity <= 0)
            {
                return BadRequest(new
                {
                    message = "Enter valid production order, lot, UOM, and consumption quantity."
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
                    return BadRequest(new
                    {
                        message = "Consumption UOM must match the production material requirement UOM."
                    });

                // Calculate total quantity issued for this lot
                // from POSTED production material issues.
                var issuedQuantity =
                    await _context.ProductionMaterialIssues
                        .Where(i =>
                            i.ProductionOrderId == dto.ProductionOrderId &&
                            i.Status == "POSTED")
                        .SelectMany(i => i.ProductionMaterialIssueLines)
                        .Where(l => l.LotId == dto.LotId)
                        .SumAsync(l => (decimal?)l.IssuedQuantity) ?? 0;

                // Calculate previous consumption for this order and lot.
                var previousLotConsumption =
                    await _context.ProductionConsumptions
                        .Where(c =>
                            c.ProductionOrderId == dto.ProductionOrderId &&
                            c.LotId == dto.LotId)
                        .SumAsync(c => (decimal?)c.ConsumedQuantity) ?? 0;

                var remainingIssuedQuantity =
                    issuedQuantity - previousLotConsumption;

                if (dto.ConsumedQuantity > remainingIssuedQuantity)
                {
                    return BadRequest(new
                    {
                        message = "Consumption quantity exceeds the remaining issued quantity for this lot.",
                        issuedQuantity,
                        previouslyConsumed = previousLotConsumption,
                        remainingQuantity = remainingIssuedQuantity,
                        requestedQuantity = dto.ConsumedQuantity
                    });
                }

                // Calculate total consumption for this item
                // across all lots in this production order.
                var previousItemConsumption =
                    await _context.ProductionConsumptions
                        .Where(c =>
                            c.ProductionOrderId == dto.ProductionOrderId &&
                            c.Lot.ItemId == lot.ItemId)
                        .SumAsync(c => (decimal?)c.ConsumedQuantity) ?? 0;

                var newItemConsumption =
                    previousItemConsumption + dto.ConsumedQuantity;

                if (newItemConsumption > requirement.IssuedQuantity)
                {
                    return BadRequest(new
                    {
                        message = "Total consumption exceeds the issued quantity for this material.",
                        issuedQuantity = requirement.IssuedQuantity,
                        previouslyConsumed = previousItemConsumption,
                        requestedQuantity = dto.ConsumedQuantity
                    });
                }

                var consumption = new ProductionConsumption
                {
                    ProductionOrderId = dto.ProductionOrderId,
                    LotId = dto.LotId,
                    ConsumedQuantity = dto.ConsumedQuantity,
                    UomId = dto.UomId,
                    ConsumptionDate = dto.ConsumptionDate ?? DateTime.UtcNow,
                    RecordedBy = dto.RecordedBy
                };

                requirement.ConsumedQuantity += dto.ConsumedQuantity;

                _context.ProductionConsumptions.Add(consumption);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = consumption.ConsumptionId },
                    new
                    {
                        message = "Production material consumption recorded successfully.",
                        consumptionId = consumption.ConsumptionId,
                        productionOrderId = order.ProductionOrderId,
                        productionNumber = order.ProductionNumber,
                        lotId = lot.LotId,
                        batchCode = lot.BatchCode,
                        itemId = lot.ItemId,
                        consumedQuantity = consumption.ConsumedQuantity,
                        uomId = consumption.UomId,
                        consumptionDate = consumption.ConsumptionDate,
                        totalConsumedForRequirement = requirement.ConsumedQuantity,
                        issuedQuantity = requirement.IssuedQuantity,
                        remainingIssuedQuantity =
                            requirement.IssuedQuantity - requirement.ConsumedQuantity
                    });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
