
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/units-of-measure")]
    [ApiController]
    public class UnitsOfMeasureController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public UnitsOfMeasureController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/units-of-measure
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var units = await _context.UnitsOfMeasures
                .AsNoTracking()
                .OrderBy(u => u.UomCode)
                .ToListAsync();

            return Ok(units);
        }

        // GET: api/units-of-measure/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var unit = await _context.UnitsOfMeasures
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UomId == id);

            if (unit == null)
            {
                return NotFound("Unit of measure not found.");
            }

            return Ok(unit);
        }

        // POST: api/units-of-measure
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UnitsOfMeasure unit)
        {
            if (unit == null)
            {
                return BadRequest("Unit of measure data is required.");
            }

            if (string.IsNullOrWhiteSpace(unit.UomCode) ||
                string.IsNullOrWhiteSpace(unit.UomName) ||
                string.IsNullOrWhiteSpace(unit.UnitType))
            {
                return BadRequest("UOM code, name, and unit type are required.");
            }

            unit.UomCode = unit.UomCode.Trim();
            unit.UomName = unit.UomName.Trim();
            unit.UnitType = unit.UnitType.Trim();

            if (unit.UomCode.Length > 20 ||
                unit.UomName.Length > 80 ||
                unit.UnitType.Length > 30)
            {
                return BadRequest(
                    "UOM code must not exceed 20 characters, " +
                    "UOM name must not exceed 80 characters, and " +
                    "unit type must not exceed 30 characters.");
            }

            var exists = await _context.UnitsOfMeasures
                .AnyAsync(u => u.UomCode == unit.UomCode);

            if (exists)
            {
                return Conflict("UOM code already exists.");
            }

            var newUnit = new UnitsOfMeasure
            {
                UomCode = unit.UomCode,
                UomName = unit.UomName,
                UnitType = unit.UnitType,
                IsActive = true
            };

            _context.UnitsOfMeasures.Add(newUnit);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("Unable to create unit of measure. The UOM code may already exist.");
            }

            return Ok(newUnit);
        }

        // PUT: api/units-of-measure/1
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] UnitsOfMeasure updatedUnit)
        {
            if (updatedUnit == null)
            {
                return BadRequest("Unit of measure data is required.");
            }

            var unit = await _context.UnitsOfMeasures
                .FirstOrDefaultAsync(u => u.UomId == id);

            if (unit == null)
            {
                return NotFound("Unit of measure not found.");
            }

            if (string.IsNullOrWhiteSpace(updatedUnit.UomCode) ||
                string.IsNullOrWhiteSpace(updatedUnit.UomName) ||
                string.IsNullOrWhiteSpace(updatedUnit.UnitType))
            {
                return BadRequest("UOM code, name, and unit type are required.");
            }

            var uomCode = updatedUnit.UomCode.Trim();
            var uomName = updatedUnit.UomName.Trim();
            var unitType = updatedUnit.UnitType.Trim();

            if (uomCode.Length > 20 ||
                uomName.Length > 80 ||
                unitType.Length > 30)
            {
                return BadRequest(
                    "UOM code must not exceed 20 characters, " +
                    "UOM name must not exceed 80 characters, and " +
                    "unit type must not exceed 30 characters.");
            }

            var duplicate = await _context.UnitsOfMeasures
                .AnyAsync(u => u.UomCode == uomCode && u.UomId != id);

            if (duplicate)
            {
                return Conflict("UOM code already exists.");
            }

            unit.UomCode = uomCode;
            unit.UomName = uomName;
            unit.UnitType = unitType;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("Unable to update unit of measure. The UOM code may already exist.");
            }

            return Ok(unit);
        }

        // PATCH: api/units-of-measure/1/deactivate
        [HttpPatch("{id:long}/deactivate")]
        public async Task<IActionResult> Deactivate(long id)
        {
            var unit = await _context.UnitsOfMeasures
                .FirstOrDefaultAsync(u => u.UomId == id);

            if (unit == null)
            {
                return NotFound("Unit of measure not found.");
            }

            if (!unit.IsActive)
            {
                return Ok(new
                {
                    message = "Unit of measure is already deactivated."
                });
            }

            unit.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Unit of measure deactivated successfully."
            });
        }
    }
}
