
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
                .OrderBy(u => u.UomCode)
                .ToListAsync();

            return Ok(units);
        }

        // GET: api/units-of-measure/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var unit = await _context.UnitsOfMeasures
                .FirstOrDefaultAsync(u => u.UomId == id);

            if (unit == null)
                return NotFound("Unit of measure not found.");

            return Ok(unit);
        }

        // POST: api/units-of-measure
        [HttpPost]
        public async Task<IActionResult> Create(UnitsOfMeasure unit)
        {
            if (string.IsNullOrWhiteSpace(unit.UomCode) ||
                string.IsNullOrWhiteSpace(unit.UomName))
            {
                return BadRequest("UOM code and name are required.");
            }

            var exists = await _context.UnitsOfMeasures
                .AnyAsync(u => u.UomCode == unit.UomCode);

            if (exists)
                return Conflict("UOM code already exists.");

            unit.UomId = 0;
            unit.IsActive = true;

            _context.UnitsOfMeasures.Add(unit);
            await _context.SaveChangesAsync();

            return Ok(unit);
        }

        // PUT: api/units-of-measure/1
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, UnitsOfMeasure updatedUnit)
        {
            var unit = await _context.UnitsOfMeasures
                .FirstOrDefaultAsync(u => u.UomId == id);

            if (unit == null)
                return NotFound("Unit of measure not found.");

            if (string.IsNullOrWhiteSpace(updatedUnit.UomCode) ||
                string.IsNullOrWhiteSpace(updatedUnit.UomName))
            {
                return BadRequest("UOM code and name are required.");
            }

            var duplicate = await _context.UnitsOfMeasures
                .AnyAsync(u => u.UomCode == updatedUnit.UomCode &&
                               u.UomId != id);

            if (duplicate)
                return Conflict("UOM code already exists.");

            unit.UomCode = updatedUnit.UomCode;
            unit.UomName = updatedUnit.UomName;
            unit.UnitType = updatedUnit.UnitType;

            await _context.SaveChangesAsync();

            return Ok(unit);
        }

        // PATCH: api/units-of-measure/1/deactivate
        [HttpPatch("{id:long}/deactivate")]
        public async Task<IActionResult> Deactivate(long id)
        {
            var unit = await _context.UnitsOfMeasures
                .FirstOrDefaultAsync(u => u.UomId == id);

            if (unit == null)
                return NotFound("Unit of measure not found.");

            unit.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Unit of measure deactivated successfully." });
        }
    }
}
