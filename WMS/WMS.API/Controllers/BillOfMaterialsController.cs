
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.BillOfMaterials;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/bill-of-materials")]
    [ApiController]
    public class BillOfMaterialsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public BillOfMaterialsController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/bill-of-materials
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var boms = await _context.BillOfMaterials
                .OrderBy(b => b.BomCode)
                .ThenBy(b => b.VersionNumber)
                .Select(b => new
                {
                    b.BomId,
                    b.BomCode,
                    b.FinishedItemId,
                    FinishedItemCode = b.FinishedItem.ItemCode,
                    FinishedItemName = b.FinishedItem.ItemName,
                    b.VersionNumber,
                    b.OutputQuantity,
                    b.OutputUomId,
                    OutputUom = b.OutputUom.UomCode,
                    b.EffectiveFrom,
                    b.EffectiveTo,
                    b.Status,
                    ComponentCount = b.BomLines.Count
                })
                .ToListAsync();

            return Ok(boms);
        }

        // GET: api/bill-of-materials/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var bom = await _context.BillOfMaterials
                .Where(b => b.BomId == id)
                .Select(b => new
                {
                    b.BomId,
                    b.BomCode,
                    b.FinishedItemId,
                    FinishedItemCode = b.FinishedItem.ItemCode,
                    FinishedItemName = b.FinishedItem.ItemName,
                    b.VersionNumber,
                    b.OutputQuantity,
                    b.OutputUomId,
                    OutputUom = b.OutputUom.UomCode,
                    b.EffectiveFrom,
                    b.EffectiveTo,
                    b.Status,

                    Lines = b.BomLines
                        .OrderBy(l => l.BomLineId)
                        .Select(l => new
                        {
                            l.BomLineId,
                            l.ComponentItemId,
                            ComponentItemCode = l.ComponentItem.ItemCode,
                            ComponentItemName = l.ComponentItem.ItemName,
                            l.RequiredQuantity,
                            l.UomId,
                            UomCode = l.Uom.UomCode,
                            l.WastagePercentage
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (bom == null)
                return NotFound(new { message = "Bill of materials not found." });

            return Ok(bom);
        }

        // POST: api/bill-of-materials
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBomDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.BomCode))
                return BadRequest(new { message = "BOM code is required." });

            if (dto.VersionNumber <= 0)
                return BadRequest(new { message = "Version number must be greater than zero." });

            if (dto.OutputQuantity <= 0)
                return BadRequest(new { message = "Output quantity must be greater than zero." });

            if (dto.EffectiveFrom.HasValue &&
                dto.EffectiveTo.HasValue &&
                dto.EffectiveTo.Value < dto.EffectiveFrom.Value)
            {
                return BadRequest(new
                {
                    message = "Effective-to date cannot be earlier than effective-from date."
                });
            }

            var finishedItem = await _context.Items
                .FirstOrDefaultAsync(i =>
                    i.ItemId == dto.FinishedItemId && i.IsActive);

            if (finishedItem == null)
                return BadRequest(new
                {
                    message = "Finished item does not exist or is inactive."
                });

            var outputUom = await _context.UnitsOfMeasures
                .FirstOrDefaultAsync(u =>
                    u.UomId == dto.OutputUomId && u.IsActive);

            if (outputUom == null)
                return BadRequest(new
                {
                    message = "Output unit of measure does not exist or is inactive."
                });

            var code = dto.BomCode.Trim();

            var duplicate = await _context.BillOfMaterials
                .AnyAsync(b =>
                    b.BomCode == code &&
                    b.VersionNumber == dto.VersionNumber);

            if (duplicate)
                return Conflict(new
                {
                    message = "BOM code and version number already exist."
                });

            var bom = new BillOfMaterial
            {
                BomCode = code,
                FinishedItemId = dto.FinishedItemId,
                VersionNumber = dto.VersionNumber,
                OutputQuantity = dto.OutputQuantity,
                OutputUomId = dto.OutputUomId,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                Status = "DRAFT"
            };

            _context.BillOfMaterials.Add(bom);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = bom.BomId },
                new
                {
                    message = "BOM created successfully.",
                    bom.BomId,
                    bom.BomCode,
                    bom.FinishedItemId,
                    bom.VersionNumber,
                    bom.OutputQuantity,
                    bom.OutputUomId,
                    bom.Status
                });
        }

        // PUT: api/bill-of-materials/1
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] UpdateBomDto dto)
        {
            var bom = await _context.BillOfMaterials
                .FirstOrDefaultAsync(b => b.BomId == id);

            if (bom == null)
                return NotFound(new { message = "BOM not found." });

            if (bom.Status != "DRAFT")
                return BadRequest(new
                {
                    message = "Only draft BOMs can be updated."
                });

            if (string.IsNullOrWhiteSpace(dto.BomCode) ||
                dto.VersionNumber <= 0 ||
                dto.OutputQuantity <= 0)
            {
                return BadRequest(new
                {
                    message = "Enter a valid BOM code, version number, and output quantity."
                });
            }

            if (dto.EffectiveFrom.HasValue &&
                dto.EffectiveTo.HasValue &&
                dto.EffectiveTo.Value < dto.EffectiveFrom.Value)
            {
                return BadRequest(new
                {
                    message = "Effective-to date cannot be earlier than effective-from date."
                });
            }

            var finishedItem = await _context.Items
                .AnyAsync(i =>
                    i.ItemId == dto.FinishedItemId && i.IsActive);

            if (!finishedItem)
                return BadRequest(new
                {
                    message = "Finished item does not exist or is inactive."
                });

            var outputUom = await _context.UnitsOfMeasures
                .AnyAsync(u =>
                    u.UomId == dto.OutputUomId && u.IsActive);

            if (!outputUom)
                return BadRequest(new
                {
                    message = "Output unit of measure does not exist or is inactive."
                });

            var code = dto.BomCode.Trim();

            var duplicate = await _context.BillOfMaterials
                .AnyAsync(b =>
                    b.BomCode == code &&
                    b.VersionNumber == dto.VersionNumber &&
                    b.BomId != id);

            if (duplicate)
                return Conflict(new
                {
                    message = "BOM code and version number already exist."
                });

            bom.BomCode = code;
            bom.FinishedItemId = dto.FinishedItemId;
            bom.VersionNumber = dto.VersionNumber;
            bom.OutputQuantity = dto.OutputQuantity;
            bom.OutputUomId = dto.OutputUomId;
            bom.EffectiveFrom = dto.EffectiveFrom;
            bom.EffectiveTo = dto.EffectiveTo;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "BOM updated successfully.",
                bom.BomId,
                bom.BomCode,
                bom.VersionNumber,
                bom.OutputQuantity,
                bom.OutputUomId,
                bom.Status
            });
        }

        // POST: api/bill-of-materials/1/lines
        [HttpPost("{id:long}/lines")]
        public async Task<IActionResult> AddLine(
            long id,
            [FromBody] AddBomLineDto dto)
        {
            var bom = await _context.BillOfMaterials
                .Include(b => b.BomLines)
                .FirstOrDefaultAsync(b => b.BomId == id);

            if (bom == null)
                return NotFound(new { message = "BOM not found." });

            if (bom.Status != "DRAFT")
                return BadRequest(new
                {
                    message = "Components can only be added to a draft BOM."
                });

            if (dto.RequiredQuantity <= 0)
                return BadRequest(new
                {
                    message = "Required quantity must be greater than zero."
                });

            if (dto.WastagePercentage < 0 || dto.WastagePercentage > 100)
                return BadRequest(new
                {
                    message = "Wastage percentage must be between 0 and 100."
                });

            if (dto.ComponentItemId == bom.FinishedItemId)
                return BadRequest(new
                {
                    message = "A finished item cannot be its own component."
                });

            var component = await _context.Items
                .FirstOrDefaultAsync(i =>
                    i.ItemId == dto.ComponentItemId && i.IsActive);

            if (component == null)
                return BadRequest(new
                {
                    message = "Component item does not exist or is inactive."
                });

            var uom = await _context.UnitsOfMeasures
                .AnyAsync(u => u.UomId == dto.UomId && u.IsActive);

            if (!uom)
                return BadRequest(new
                {
                    message = "Component unit of measure does not exist or is inactive."
                });

            var duplicate = bom.BomLines.Any(l =>
                l.ComponentItemId == dto.ComponentItemId);

            if (duplicate)
                return Conflict(new
                {
                    message = "This component already exists in the BOM."
                });

            var line = new BomLine
            {
                BomId = id,
                ComponentItemId = dto.ComponentItemId,
                RequiredQuantity = dto.RequiredQuantity,
                UomId = dto.UomId,
                WastagePercentage = dto.WastagePercentage
            };

            _context.BomLines.Add(line);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "BOM component added successfully.",
                line.BomLineId,
                line.BomId,
                line.ComponentItemId,
                ComponentItemCode = component.ItemCode,
                ComponentItemName = component.ItemName,
                line.RequiredQuantity,
                line.UomId,
                line.WastagePercentage
            });
        }

        // PUT: api/bill-of-materials/1/lines/1
        [HttpPut("{id:long}/lines/{lineId:long}")]
        public async Task<IActionResult> UpdateLine(
            long id,
            long lineId,
            [FromBody] UpdateBomLineDto dto)
        {
            var bom = await _context.BillOfMaterials
                .FirstOrDefaultAsync(b => b.BomId == id);

            if (bom == null)
                return NotFound(new { message = "BOM not found." });

            if (bom.Status != "DRAFT")
                return BadRequest(new
                {
                    message = "Only draft BOM components can be updated."
                });

            var line = await _context.BomLines
                .FirstOrDefaultAsync(l =>
                    l.BomLineId == lineId && l.BomId == id);

            if (line == null)
                return NotFound(new { message = "BOM component not found." });

            if (dto.RequiredQuantity <= 0)
                return BadRequest(new
                {
                    message = "Required quantity must be greater than zero."
                });

            if (dto.WastagePercentage < 0 || dto.WastagePercentage > 100)
                return BadRequest(new
                {
                    message = "Wastage percentage must be between 0 and 100."
                });

            if (dto.ComponentItemId == bom.FinishedItemId)
                return BadRequest(new
                {
                    message = "A finished item cannot be its own component."
                });

            var componentExists = await _context.Items
                .AnyAsync(i =>
                    i.ItemId == dto.ComponentItemId && i.IsActive);

            if (!componentExists)
                return BadRequest(new
                {
                    message = "Component item does not exist or is inactive."
                });

            var uomExists = await _context.UnitsOfMeasures
                .AnyAsync(u => u.UomId == dto.UomId && u.IsActive);

            if (!uomExists)
                return BadRequest(new
                {
                    message = "Component unit of measure does not exist or is inactive."
                });

            var duplicate = await _context.BomLines
                .AnyAsync(l =>
                    l.BomId == id &&
                    l.ComponentItemId == dto.ComponentItemId &&
                    l.BomLineId != lineId);

            if (duplicate)
                return Conflict(new
                {
                    message = "This component already exists in the BOM."
                });

            line.ComponentItemId = dto.ComponentItemId;
            line.RequiredQuantity = dto.RequiredQuantity;
            line.UomId = dto.UomId;
            line.WastagePercentage = dto.WastagePercentage;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "BOM component updated successfully.",
                line.BomLineId,
                line.BomId,
                line.ComponentItemId,
                line.RequiredQuantity,
                line.UomId,
                line.WastagePercentage
            });
        }

        // DELETE: api/bill-of-materials/1/lines/1
        [HttpDelete("{id:long}/lines/{lineId:long}")]
        public async Task<IActionResult> DeleteLine(long id, long lineId)
        {
            var bom = await _context.BillOfMaterials
                .FirstOrDefaultAsync(b => b.BomId == id);

            if (bom == null)
                return NotFound(new { message = "BOM not found." });

            if (bom.Status != "DRAFT")
                return BadRequest(new
                {
                    message = "Components can only be removed from a draft BOM."
                });

            var line = await _context.BomLines
                .FirstOrDefaultAsync(l =>
                    l.BomLineId == lineId && l.BomId == id);

            if (line == null)
                return NotFound(new { message = "BOM component not found." });

            _context.BomLines.Remove(line);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "BOM component removed successfully."
            });
        }

        // POST: api/bill-of-materials/1/activate
        [HttpPost("{id:long}/activate")]
        public async Task<IActionResult> Activate(long id)
        {
            var bom = await _context.BillOfMaterials
                .Include(b => b.FinishedItem)
                    .ThenInclude(i => i.Category)
                .Include(b => b.OutputUom)
                .Include(b => b.BomLines)
                    .ThenInclude(l => l.ComponentItem)
                .Include(b => b.BomLines)
                    .ThenInclude(l => l.Uom)
                .FirstOrDefaultAsync(b => b.BomId == id);

            if (bom == null)
                return NotFound(new { message = "BOM not found." });

            if (bom.Status != "DRAFT")
                return BadRequest(new
                {
                    message = "Only draft BOMs can be activated."
                });

            // Check if BOM has components
            if (!bom.BomLines.Any())
                return BadRequest(new
                {
                    message = "BOM must have at least one component before activation."
                });

            // Check finished item
            if (!bom.FinishedItem.IsActive)
                return BadRequest(new
                {
                    message = "Finished item is inactive."
                });

            if (bom.FinishedItem.Category.CategoryCode != "FG" ||
                !bom.FinishedItem.Category.IsActive)
                return BadRequest(new
                {
                    message = "Finished item must belong to an active Finished Goods category."
                });

            // Check output UOM
            if (!bom.OutputUom.IsActive)
                return BadRequest(new
                {
                    message = "Output unit of measure is inactive."
                });

            // Check effective dates
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (!bom.EffectiveFrom.HasValue)
                return BadRequest(new
                {
                    message = "Effective-from date is required before activation."
                });

            if (bom.EffectiveFrom.Value > today)
                return BadRequest(new
                {
                    message = "BOM cannot be activated before its effective-from date."
                });

            if (bom.EffectiveTo.HasValue &&
                bom.EffectiveTo.Value < today)
                return BadRequest(new
                {
                    message = "BOM effective-to date has already passed."
                });

            // Check every component
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
                        message = $"Unit of measure for component {line.ComponentItem.ItemCode} is inactive."
                    });
            }

            // Activate BOM
            bom.Status = "ACTIVE";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "BOM activated successfully.",
                bom.BomId,
                bom.BomCode,
                bom.FinishedItemId,
                FinishedItemCode = bom.FinishedItem.ItemCode,
                bom.VersionNumber,
                bom.OutputQuantity,
                bom.OutputUomId,
                bom.Status,
                ComponentCount = bom.BomLines.Count
            });
        }

    }
}
