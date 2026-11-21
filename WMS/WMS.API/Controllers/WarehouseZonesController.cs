
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs;
using WMS.API.Models;

namespace WMS.API.Controllers;

[Route("api/warehouses/{warehouseId:long}/zones")]
[ApiController]
public class WarehouseZonesController : ControllerBase
{
    private readonly WmsDbContext _context;

    public WarehouseZonesController(WmsDbContext context)
    {
        _context = context;
    }

    // GET: api/warehouses/1/zones
    [HttpGet]
    public async Task<IActionResult> GetZones(long warehouseId)
    {
        var warehouseExists = await _context.Warehouses
            .AsNoTracking()
            .AnyAsync(w => w.WarehouseId == warehouseId);

        if (!warehouseExists)
        {
            return NotFound(new { message = "Warehouse not found." });
        }

        var zones = await _context.WarehouseZones
            .AsNoTracking()
            .Where(z => z.WarehouseId == warehouseId)
            .OrderBy(z => z.ZoneCode)
            .ToListAsync();

        return Ok(zones);
    }

    // GET: api/warehouses/1/zones/1
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetZone(long warehouseId, long id)
    {
        var zone = await _context.WarehouseZones
            .AsNoTracking()
            .FirstOrDefaultAsync(z =>
                z.WarehouseId == warehouseId &&
                z.ZoneId == id);

        if (zone == null)
        {
            return NotFound(new { message = "Warehouse zone not found." });
        }

        return Ok(zone);
    }

    // POST: api/warehouses/1/zones
    [HttpPost]
    public async Task<IActionResult> CreateZone(
        long warehouseId,
        [FromBody] CreateWarehouseZoneDto dto)
    {
        if (dto == null)
        {
            return BadRequest(new { message = "Zone data is required." });
        }

        var zoneCode = dto.ZoneCode?.Trim();
        var zoneName = dto.ZoneName?.Trim();

        if (string.IsNullOrWhiteSpace(zoneCode) ||
            string.IsNullOrWhiteSpace(zoneName))
        {
            return BadRequest(new
            {
                message = "Zone code and zone name are required."
            });
        }

        if (zoneCode.Length > 40 || zoneName.Length > 100)
        {
            return BadRequest(new
            {
                message = "Zone code must not exceed 40 characters and zone name must not exceed 100 characters."
            });
        }

        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.WarehouseId == warehouseId);

        if (!warehouseExists)
        {
            return NotFound(new { message = "Warehouse not found." });
        }

        var existingZone = await _context.WarehouseZones
            .AnyAsync(z =>
                z.WarehouseId == warehouseId &&
                z.ZoneCode == zoneCode);

        if (existingZone)
        {
            return Conflict(new
            {
                message = "Zone code already exists in this warehouse."
            });
        }

        var zone = new WarehouseZone
        {
            WarehouseId = warehouseId,
            ZoneCode = zoneCode,
            ZoneName = zoneName
        };

        _context.WarehouseZones.Add(zone);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Unable to create zone. The zone code may already exist in this warehouse."
            });
        }

        return CreatedAtAction(
            nameof(GetZone),
            new { warehouseId, id = zone.ZoneId },
            zone);
    }

    // PUT: api/warehouses/1/zones/1
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateZone(
        long warehouseId,
        long id,
        [FromBody] UpdateWarehouseZoneDto dto)
    {
        if (dto == null)
        {
            return BadRequest(new { message = "Zone data is required." });
        }

        var zoneName = dto.ZoneName?.Trim();

        if (string.IsNullOrWhiteSpace(zoneName))
        {
            return BadRequest(new { message = "Zone name is required." });
        }

        if (zoneName.Length > 100)
        {
            return BadRequest(new
            {
                message = "Zone name must not exceed 100 characters."
            });
        }

        var zone = await _context.WarehouseZones
            .FirstOrDefaultAsync(z =>
                z.WarehouseId == warehouseId &&
                z.ZoneId == id);

        if (zone == null)
        {
            return NotFound(new { message = "Warehouse zone not found." });
        }

        zone.ZoneName = zoneName;

        await _context.SaveChangesAsync();

        return Ok(zone);
    }
}
