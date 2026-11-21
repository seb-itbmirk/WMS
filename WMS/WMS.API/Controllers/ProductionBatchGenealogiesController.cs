
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.ProductionBatchGenealogies;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/production-batch-genealogies")]
    [ApiController]
    public class ProductionBatchGenealogiesController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public ProductionBatchGenealogiesController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/production-batch-genealogies
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var records = await _context.ProductionBatchGenealogies
                .AsNoTracking()
                .OrderByDescending(g => g.GenealogyId)
                .Select(g => new
                {
                    g.GenealogyId,
                    g.ProductionOrderId,
                    g.ProductionOrder.ProductionNumber,

                    g.InputLotId,
                    InputBatchCode = g.InputLot.BatchCode,
                    InputItemCode = g.InputLot.Item.ItemCode,
                    InputItemName = g.InputLot.Item.ItemName,

                    g.OutputLotId,
                    OutputBatchCode = g.OutputLot.BatchCode,
                    OutputItemCode = g.OutputLot.Item.ItemCode,
                    OutputItemName = g.OutputLot.Item.ItemName,

                    g.ConsumedQuantity,
                    g.ProducedQuantity,
                    g.UomId,
                    UomCode = g.Uom.UomCode
                })
                .ToListAsync();

            return Ok(records);
        }

        // GET: api/production-batch-genealogies/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var record = await _context.ProductionBatchGenealogies
                .AsNoTracking()
                .Where(g => g.GenealogyId == id)
                .Select(g => new
                {
                    g.GenealogyId,
                    g.ProductionOrderId,
                    g.ProductionOrder.ProductionNumber,

                    g.InputLotId,
                    InputBatchCode = g.InputLot.BatchCode,
                    InputItemCode = g.InputLot.Item.ItemCode,
                    InputItemName = g.InputLot.Item.ItemName,

                    g.OutputLotId,
                    OutputBatchCode = g.OutputLot.BatchCode,
                    OutputItemCode = g.OutputLot.Item.ItemCode,
                    OutputItemName = g.OutputLot.Item.ItemName,

                    g.ConsumedQuantity,
                    g.ProducedQuantity,
                    g.UomId,
                    UomCode = g.Uom.UomCode
                })
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return NotFound(new
                {
                    message = "Production batch genealogy record not found."
                });
            }

            return Ok(record);
        }

        // GET: api/production-batch-genealogies/production-order/1
        [HttpGet("production-order/{productionOrderId:long}")]
        public async Task<IActionResult> GetByProductionOrder(
            long productionOrderId)
        {
            var orderExists = await _context.ProductionOrders
                .AnyAsync(p => p.ProductionOrderId == productionOrderId);

            if (!orderExists)
            {
                return NotFound(new
                {
                    message = "Production order not found."
                });
            }

            var records = await _context.ProductionBatchGenealogies
                .AsNoTracking()
                .Where(g => g.ProductionOrderId == productionOrderId)
                .OrderBy(g => g.InputLotId)
                .ThenBy(g => g.OutputLotId)
                .Select(g => new
                {
                    g.GenealogyId,

                    g.InputLotId,
                    InputBatchCode = g.InputLot.BatchCode,
                    InputItemCode = g.InputLot.Item.ItemCode,
                    InputItemName = g.InputLot.Item.ItemName,

                    g.OutputLotId,
                    OutputBatchCode = g.OutputLot.BatchCode,
                    OutputItemCode = g.OutputLot.Item.ItemCode,
                    OutputItemName = g.OutputLot.Item.ItemName,

                    g.ConsumedQuantity,
                    g.ProducedQuantity,
                    g.UomId,
                    UomCode = g.Uom.UomCode
                })
                .ToListAsync();

            return Ok(records);
        }

        // GET: api/production-batch-genealogies/output-lot/5
        [HttpGet("output-lot/{outputLotId:long}")]
        public async Task<IActionResult> GetByOutputLot(long outputLotId)
        {
            var lotExists = await _context.InventoryLots
                .AnyAsync(l => l.LotId == outputLotId);

            if (!lotExists)
            {
                return NotFound(new
                {
                    message = "Output lot not found."
                });
            }

            var records = await _context.ProductionBatchGenealogies
                .AsNoTracking()
                .Where(g => g.OutputLotId == outputLotId)
                .OrderBy(g => g.InputLotId)
                .Select(g => new
                {
                    g.GenealogyId,
                    g.ProductionOrderId,
                    g.ProductionOrder.ProductionNumber,

                    g.InputLotId,
                    InputBatchCode = g.InputLot.BatchCode,
                    InputItemCode = g.InputLot.Item.ItemCode,
                    InputItemName = g.InputLot.Item.ItemName,

                    g.OutputLotId,
                    OutputBatchCode = g.OutputLot.BatchCode,
                    OutputItemCode = g.OutputLot.Item.ItemCode,
                    OutputItemName = g.OutputLot.Item.ItemName,

                    g.ConsumedQuantity,
                    g.ProducedQuantity,
                    g.UomId,
                    UomCode = g.Uom.UomCode
                })
                .ToListAsync();

            return Ok(records);
        }

        // POST: api/production-batch-genealogies
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductionBatchGenealogyDto dto)
        {
            if (dto.ProductionOrderId <= 0 ||
                dto.InputLotId <= 0 ||
                dto.OutputLotId <= 0 ||
                dto.ConsumedQuantity <= 0 ||
                dto.UomId <= 0 ||
                (dto.ProducedQuantity.HasValue &&
                 dto.ProducedQuantity.Value <= 0))
            {
                return BadRequest(new
                {
                    message = "Enter valid genealogy details and positive quantities."
                });
            }

            if (dto.InputLotId == dto.OutputLotId)
            {
                return BadRequest(new
                {
                    message = "Input lot and output lot cannot be the same."
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
                {
                    return NotFound(new
                    {
                        message = "Production order not found."
                    });
                }

                var inputLot = await _context.InventoryLots
                    .FirstOrDefaultAsync(l => l.LotId == dto.InputLotId);

                if (inputLot == null)
                {
                    return NotFound(new
                    {
                        message = "Input lot not found."
                    });
                }

                var outputLot = await _context.InventoryLots
                    .FirstOrDefaultAsync(l => l.LotId == dto.OutputLotId);

                if (outputLot == null)
                {
                    return NotFound(new
                    {
                        message = "Output lot not found."
                    });
                }

                if (outputLot.ItemId != order.FinishedItemId)
                {
                    return BadRequest(new
                    {
                        message = "Output lot must belong to the production order's finished product."
                    });
                }

                var outputExists = await _context.ProductionOutputs
                    .AnyAsync(o =>
                        o.ProductionOrderId == dto.ProductionOrderId &&
                        o.FinishedLotId == dto.OutputLotId);

                if (!outputExists)
                {
                    return BadRequest(new
                    {
                        message = "Output lot is not registered as an output of this production order."
                    });
                }

                var requirement =
                    await _context.ProductionMaterialRequirements
                        .FirstOrDefaultAsync(r =>
                            r.ProductionOrderId == dto.ProductionOrderId &&
                            r.ItemId == inputLot.ItemId);

                if (requirement == null)
                {
                    return BadRequest(new
                    {
                        message = "Input lot's item is not part of this production order."
                    });
                }

                if (dto.UomId != requirement.UomId)
                {
                    return BadRequest(new
                    {
                        message = "Genealogy UOM must match the input material requirement UOM."
                    });
                }

                // Total quantity consumed from this input lot.
                var consumedForLot =
                    await _context.ProductionConsumptions
                        .Where(c =>
                            c.ProductionOrderId == dto.ProductionOrderId &&
                            c.LotId == dto.InputLotId)
                        .SumAsync(c =>
                            (decimal?)c.ConsumedQuantity) ?? 0m;

                // Quantity from this input lot already linked to output lots.
                var alreadyLinked =
                    await _context.ProductionBatchGenealogies
                        .Where(g =>
                            g.ProductionOrderId == dto.ProductionOrderId &&
                            g.InputLotId == dto.InputLotId)
                        .SumAsync(g =>
                            (decimal?)g.ConsumedQuantity) ?? 0m;

                if (alreadyLinked + dto.ConsumedQuantity > consumedForLot)
                {
                    return BadRequest(new
                    {
                        message = "Genealogy quantity cannot exceed the consumed quantity recorded for this input lot.",
                        consumedQuantity = consumedForLot,
                        alreadyLinkedQuantity = alreadyLinked,
                        requestedQuantity = dto.ConsumedQuantity,
                        remainingQuantity =
                            consumedForLot - alreadyLinked
                    });
                }

                // Total quantity produced in this output lot.
                var outputQuantity =
                    await _context.ProductionOutputs
                        .Where(o =>
                            o.ProductionOrderId == dto.ProductionOrderId &&
                            o.FinishedLotId == dto.OutputLotId)
                        .SumAsync(o =>
                            (decimal?)o.ProducedQuantity) ?? 0m;

                // Quantity from this output lot already linked to input lots.
                var alreadyProducedLinked =
                    await _context.ProductionBatchGenealogies
                        .Where(g =>
                            g.ProductionOrderId == dto.ProductionOrderId &&
                            g.OutputLotId == dto.OutputLotId)
                        .SumAsync(g =>
                            (decimal?)g.ProducedQuantity) ?? 0m;

                if (dto.ProducedQuantity.HasValue &&
                    alreadyProducedLinked + dto.ProducedQuantity.Value >
                    outputQuantity)
                {
                    return BadRequest(new
                    {
                        message = "Linked produced quantity cannot exceed the production output quantity.",
                        outputQuantity,
                        alreadyLinkedQuantity = alreadyProducedLinked,
                        requestedQuantity = dto.ProducedQuantity.Value,
                        remainingQuantity =
                            outputQuantity - alreadyProducedLinked
                    });
                }

                var genealogy = new ProductionBatchGenealogy
                {
                    ProductionOrderId = dto.ProductionOrderId,
                    InputLotId = dto.InputLotId,
                    OutputLotId = dto.OutputLotId,
                    ConsumedQuantity = dto.ConsumedQuantity,
                    ProducedQuantity = dto.ProducedQuantity,
                    UomId = dto.UomId
                };

                _context.ProductionBatchGenealogies.Add(genealogy);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = genealogy.GenealogyId },
                    new
                    {
                        message = "Production batch genealogy recorded successfully.",
                        genealogyId = genealogy.GenealogyId,
                        productionOrderId = order.ProductionOrderId,
                        productionNumber = order.ProductionNumber,

                        inputLotId = inputLot.LotId,
                        inputBatchCode = inputLot.BatchCode,
                        inputItemId = inputLot.ItemId,

                        outputLotId = outputLot.LotId,
                        outputBatchCode = outputLot.BatchCode,
                        outputItemId = outputLot.ItemId,

                        consumedQuantity = genealogy.ConsumedQuantity,
                        producedQuantity = genealogy.ProducedQuantity,
                        uomId = genealogy.UomId
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
