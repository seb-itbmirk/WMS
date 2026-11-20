
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


        // GET: api/production-orders/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var statusCounts = await _context.ProductionOrders
                .GroupBy(p => p.ProductionStatus)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var totalOrders = await _context.ProductionOrders.CountAsync();

            var completedOrders = await _context.ProductionOrders
                .CountAsync(p => p.ProductionStatus == "COMPLETED");

            var inProgressOrders = await _context.ProductionOrders
                .CountAsync(p => p.ProductionStatus == "IN_PROGRESS");

            var releasedOrders = await _context.ProductionOrders
                .CountAsync(p => p.ProductionStatus == "RELEASED");

            var draftOrders = await _context.ProductionOrders
                .CountAsync(p => p.ProductionStatus == "DRAFT");

            var cancelledOrders = await _context.ProductionOrders
                .CountAsync(p => p.ProductionStatus == "CANCELLED");

            return Ok(new
            {
                TotalOrders = totalOrders,
                CompletedOrders = completedOrders,
                InProgressOrders = inProgressOrders,
                ReleasedOrders = releasedOrders,
                DraftOrders = draftOrders,
                CancelledOrders = cancelledOrders,
                StatusBreakdown = statusCounts
            });
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


        // GET: api/production-orders/1/progress
        [HttpGet("{id:long}/progress")]
        public async Task<IActionResult> GetProgress(long id)
        {
            var order = await _context.ProductionOrders
                .Where(p => p.ProductionOrderId == id)
                .Select(p => new
                {
                    p.ProductionOrderId,
                    p.ProductionNumber,
                    p.PlannedQuantity,
                    p.ProductionStatus,
                    FinishedItemCode = p.FinishedItem.ItemCode,
                    FinishedItemName = p.FinishedItem.ItemName,
                    UomCode = p.Uom.UomCode
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(new
                {
                    message = "Production order not found."
                });
            }

            var producedQuantity = await _context.ProductionOutputs
                .Where(o => o.ProductionOrderId == id)
                .SumAsync(o => (decimal?)o.ProducedQuantity) ?? 0;

            var remainingQuantity = order.PlannedQuantity - producedQuantity;

            var completionPercentage = order.PlannedQuantity > 0
                ? (producedQuantity / order.PlannedQuantity) * 100
                : 0;

            return Ok(new
            {
                order.ProductionOrderId,
                order.ProductionNumber,
                order.FinishedItemCode,
                order.FinishedItemName,
                order.PlannedQuantity,
                ProducedQuantity = producedQuantity,
                RemainingQuantity = Math.Max(0, remainingQuantity),
                CompletionPercentage = Math.Round(completionPercentage, 2),
                order.UomCode,
                order.ProductionStatus
            });
        }


        // GET: api/production-orders/1/materials-summary
        [HttpGet("{id:long}/materials-summary")]
        public async Task<IActionResult> GetMaterialsSummary(long id)
        {
            var order = await _context.ProductionOrders
                .Include(p => p.ProductionMaterialRequirements)
                    .ThenInclude(r => r.Item)
                .FirstOrDefaultAsync(p => p.ProductionOrderId == id);

            if (order == null)
            {
                return NotFound(new
                {
                    message = "Production order not found."
                });
            }

            var consumptionByItem = await _context.ProductionConsumptions
                .Where(c => c.ProductionOrderId == id)
                .GroupBy(c => c.Lot.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    TotalConsumed = g.Sum(c => c.ConsumedQuantity)
                })
                .ToListAsync();

            var wasteByItem = await _context.ProductionWastes
                .Where(w => w.ProductionOrderId == id)
                .GroupBy(w => w.Lot.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    TotalWasted = g.Sum(w => w.WastedQuantity)
                })
                .ToListAsync();

            var materialSummary = order.ProductionMaterialRequirements
                .Select(requirement =>
                {
                    var consumed = consumptionByItem
                        .FirstOrDefault(c => c.ItemId == requirement.ItemId)
                        ?.TotalConsumed ?? 0;

                    var wasted = wasteByItem
                        .FirstOrDefault(w => w.ItemId == requirement.ItemId)
                        ?.TotalWasted ?? 0;

                    var unaccounted = requirement.IssuedQuantity
                        - consumed - wasted;

                    return new
                    {
                        requirement.ItemId,
                        ItemCode = requirement.Item.ItemCode,
                        ItemName = requirement.Item.ItemName,
                        requirement.RequiredQuantity,
                        requirement.ReservedQuantity,
                        requirement.IssuedQuantity,
                        ConsumedQuantity = consumed,
                        WastedQuantity = wasted,
                        UnaccountedQuantity = unaccounted,
                        NotYetIssuedQuantity = Math.Max(
                            0,
                            requirement.RequiredQuantity - requirement.IssuedQuantity
                        )
                    };
                })
                .ToList();

            return Ok(new
            {
                order.ProductionOrderId,
                order.ProductionNumber,
                order.ProductionStatus,
                MaterialCount = materialSummary.Count,
                Materials = materialSummary
            });
        }


        // GET: api/production-orders/history

        [HttpGet("history")]
        public async Task<IActionResult> GetProductionHistory(
            [FromQuery] string? status,
            [FromQuery] DateOnly? startDate,
            [FromQuery] DateOnly? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1)
                return BadRequest(new { message = "Page must be at least 1." });

            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { message = "Page size must be between 1 and 100." });

            if (startDate.HasValue && endDate.HasValue &&
                endDate.Value < startDate.Value)
                return BadRequest(new { message = "End date cannot be earlier than start date." });

            var query = _context.ProductionOrders
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p =>
                    p.ProductionStatus == status.Trim().ToUpper());
            }

            if (startDate.HasValue)
            {
                query = query.Where(p =>
                    p.CreatedAt >= startDate.Value.ToDateTime(TimeOnly.MinValue));
            }

            if (endDate.HasValue)
            {
                var exclusiveEndDate = endDate.Value.AddDays(1)
                    .ToDateTime(TimeOnly.MinValue);

                query = query.Where(p => p.CreatedAt < exclusiveEndDate);
            }

            var totalRecords = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                (double)totalRecords / pageSize);

            var history = await query
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.ProductionOrderId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.ProductionOrderId,
                    p.ProductionNumber,
                    p.ProductionStatus,
                    p.CreatedAt,
                    p.PlannedStartDate,
                    p.PlannedEndDate,
                    p.ActualStartDate,
                    p.ActualEndDate,
                    p.PlannedQuantity,
                    UomCode = p.Uom.UomCode,
                    FinishedItemCode = p.FinishedItem.ItemCode,
                    FinishedItemName = p.FinishedItem.ItemName,
                    WarehouseName = p.Warehouse.WarehouseName
                })
                .ToListAsync();

            return Ok(new
            {
                TotalRecords = totalRecords,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages,
                Records = history
            });
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
