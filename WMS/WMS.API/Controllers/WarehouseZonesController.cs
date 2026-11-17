
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;
using WMS.API.DTOs;

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
            .AnyAsync(w => w.WarehouseId == warehouseId);

        if (!warehouseExists)
        {
            return NotFound(new { message = "Warehouse not found." });
        }

        var zones = await _context.WarehouseZones
            .Where(z => z.WarehouseId == warehouseId)
            .ToListAsync();

        return Ok(zones);
    }

    // GET: api/warehouses/1/zones/1
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetZone(long warehouseId, long id)
    {
        var zone = await _context.WarehouseZones
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
        long warehouseId, CreateWarehouseZoneDto dto)
    {
        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.WarehouseId == warehouseId);

        if (!warehouseExists)
        {
            return NotFound(new { message = "Warehouse not found." });
        }

        var zoneCode = dto.ZoneCode.Trim();

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
            ZoneName = dto.ZoneName.Trim()
        };

        _context.WarehouseZones.Add(zone);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetZone),
            new { warehouseId = warehouseId, id = zone.ZoneId },
            zone
        );
    }

    // PUT: api/warehouses/1/zones/1
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateZone(
        long warehouseId, long id, UpdateWarehouseZoneDto dto)
    {
        var zone = await _context.WarehouseZones
            .FirstOrDefaultAsync(z =>
                z.WarehouseId == warehouseId &&
                z.ZoneId == id);

        if (zone == null)
        {
            return NotFound(new { message = "Warehouse zone not found." });
        }

        zone.ZoneName = dto.ZoneName.Trim();

        await _context.SaveChangesAsync();

        return Ok(zone);
    }
}
