
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.ProductionOrders;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/production-orders")]
    [ApiController]
    public class ProductionOrdersController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public ProductionOrdersController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/production-orders
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.ProductionOrders
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.ProductionOrderId,
                    p.ProductionNumber,
                    p.BomId,
                    BomCode = p.Bom.BomCode,
                    p.WarehouseId,
                    WarehouseName = p.Warehouse.WarehouseName,
                    p.FinishedItemId,
                    FinishedItemCode = p.FinishedItem.ItemCode,
                    FinishedItemName = p.FinishedItem.ItemName,
                    p.PlannedQuantity,
                    p.UomId,
                    UomCode = p.Uom.UomCode,
                    p.PlannedStartDate,
                    p.PlannedEndDate,
                    p.ActualStartDate,
                    p.ActualEndDate,
                    p.ProductionStatus,
                    p.CreatedAt,
                    MaterialCount = p.ProductionMaterialRequirements.Count
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/production-orders/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var order = await _context.ProductionOrders
                .Where(p => p.ProductionOrderId == id)
                .Select(p => new
                {
                    p.ProductionOrderId,
                    p.ProductionNumber,
                    p.BomId,
                    BomCode = p.Bom.BomCode,
                    p.WarehouseId,
                    WarehouseName = p.Warehouse.WarehouseName,
                    p.FinishedItemId,
                    FinishedItemCode = p.FinishedItem.ItemCode,
                    FinishedItemName = p.FinishedItem.ItemName,
                    p.PlannedQuantity,
                    p.UomId,
                    UomCode = p.Uom.UomCode,
                    p.PlannedStartDate,
                    p.PlannedEndDate,
                    p.ActualStartDate,
                    p.ActualEndDate,
                    p.ProductionStatus,
                    p.CreatedBy,
                    p.CreatedAt,

                    MaterialRequirements = p.ProductionMaterialRequirements
                        .OrderBy(r => r.RequirementId)
                        .Select(r => new
                        {
                            r.RequirementId,
                            r.ItemId,
                            ItemCode = r.Item.ItemCode,
                            ItemName = r.Item.ItemName,
                            r.RequiredQuantity,
                            r.ReservedQuantity,
                            r.IssuedQuantity,
                            r.ConsumedQuantity,
                            r.UomId,
                            UomCode = r.Uom.UomCode
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound(new
                {
                    message = "Production order not found."
                });

            return Ok(order);
        }

        // POST: api/production-orders
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductionOrderDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductionNumber))
                return BadRequest(new
                {
                    message = "Production number is required."
                });

            if (dto.PlannedQuantity <= 0)
                return BadRequest(new
                {
                    message = "Planned quantity must be greater than zero."
                });

            if (dto.PlannedStartDate.HasValue &&
                dto.PlannedEndDate.HasValue &&
                dto.PlannedEndDate.Value < dto.PlannedStartDate.Value)
                return BadRequest(new
                {
                    message = "Planned end date cannot be earlier than planned start date."
                });

            var productionNumber = dto.ProductionNumber.Trim();

            var duplicateNumber = await _context.ProductionOrders
                .AnyAsync(p => p.ProductionNumber == productionNumber);

            if (duplicateNumber)
                return Conflict(new
                {
                    message = "Production number already exists."
                });

            // Find active BOM and its components
            var bom = await _context.BillOfMaterials
                .Include(b => b.FinishedItem)
                    .ThenInclude(i => i.Category)
                .Include(b => b.OutputUom)
                .Include(b => b.BomLines)
                    .ThenInclude(l => l.ComponentItem)
                .Include(b => b.BomLines)
                    .ThenInclude(l => l.Uom)
                .FirstOrDefaultAsync(b => b.BomId == dto.BomId);

            if (bom == null)
                return BadRequest(new
                {
                    message = "BOM does not exist."
                });

            if (bom.Status != "ACTIVE")
                return BadRequest(new
                {
                    message = "Only active BOMs can be used for production."
                });

            if (!bom.BomLines.Any())
                return BadRequest(new
                {
                    message = "BOM has no components."
                });

            if (!bom.FinishedItem.IsActive ||
                !bom.FinishedItem.Category.IsActive ||
                bom.FinishedItem.Category.CategoryCode != "FG")
                return BadRequest(new
                {
                    message = "BOM must reference an active Finished Goods item."
                });

            // Validate BOM effective dates against planned start date
            var productionDate = dto.PlannedStartDate ??
                DateOnly.FromDateTime(DateTime.UtcNow);

            if (bom.EffectiveFrom.HasValue &&
                productionDate < bom.EffectiveFrom.Value)
                return BadRequest(new
                {
                    message = "BOM is not yet effective for the planned production date."
                });

            if (bom.EffectiveTo.HasValue &&
                productionDate > bom.EffectiveTo.Value)
                return BadRequest(new
                {
                    message = "BOM has expired for the planned production date."
                });

            if (!bom.OutputUom.IsActive)
                return BadRequest(new
                {
                    message = "BOM output unit of measure is inactive."
                });

            // Validate warehouse
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w =>
                    w.WarehouseId == dto.WarehouseId &&
                    w.IsActive);

            if (warehouse == null)
                return BadRequest(new
                {
                    message = "Warehouse does not exist or is inactive."
                });

            // Validate BOM components
            foreach (var line in bom.BomLines)
            {
                if (!line.ComponentItem.IsActive)
                    return BadRequest(new
                    {
                        message = $"Component {line.ComponentItem.ItemCode} is inactive."
                    });

                if (!line.Uom.IsActive)
                    return BadRequest(new
                    {
                        message = $"UOM for component {line.ComponentItem.ItemCode} is inactive."
                    });
            }

            // Calculate material requirements
            var requirements = bom.BomLines.Select(line =>
                new ProductionMaterialRequirement
                {
                    ItemId = line.ComponentItemId,

                    RequiredQuantity =
                        (dto.PlannedQuantity / bom.OutputQuantity) *
                        line.RequiredQuantity *
                        (1 + (line.WastagePercentage / 100)),

                    ReservedQuantity = 0,
                    IssuedQuantity = 0,
                    ConsumedQuantity = 0,
                    UomId = line.UomId
                })
                .ToList();

            // Save production order and requirements together
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new ProductionOrder
                {
                    ProductionNumber = productionNumber,
                    BomId = bom.BomId,
                    WarehouseId = dto.WarehouseId,
                    FinishedItemId = bom.FinishedItemId,
                    PlannedQuantity = dto.PlannedQuantity,
                    UomId = bom.OutputUomId,
                    PlannedStartDate = dto.PlannedStartDate,
                    PlannedEndDate = dto.PlannedEndDate,
                    ProductionStatus = "DRAFT",
                    CreatedBy = dto.CreatedBy,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductionOrders.Add(order);

                await _context.SaveChangesAsync();

                foreach (var requirement in requirements)
                {
                    requirement.ProductionOrderId =
                        order.ProductionOrderId;
                }

                _context.ProductionMaterialRequirements.AddRange(
                    requirements);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = order.ProductionOrderId },
                    new
                    {
                        message = "Production order created successfully.",
                        order.ProductionOrderId,
                        order.ProductionNumber,
                        order.BomId,
                        order.WarehouseId,
                        order.FinishedItemId,
                        order.PlannedQuantity,
                        order.UomId,
                        order.ProductionStatus,
                        MaterialCount = requirements.Count
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
