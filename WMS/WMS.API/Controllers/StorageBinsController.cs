
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;
using WMS.API.DTOs;

namespace WMS.API.Controllers;

[Route("api/warehouses/{warehouseId:long}/zones/{zoneId:long}/bins")]
[ApiController]
public class StorageBinsController : ControllerBase
{
    private readonly WmsDbContext _context;

    public StorageBinsController(WmsDbContext context)
    {
        _context = context;
    }

    // Checks whether the zone belongs to the specified warehouse
    private async Task<bool> ZoneExists(long warehouseId, long zoneId)
    {
        return await _context.WarehouseZones.AnyAsync(z =>
            z.WarehouseId == warehouseId &&
            z.ZoneId == zoneId);
    }

    // GET: api/warehouses/1/zones/2/bins
    [HttpGet]
    public async Task<IActionResult> GetBins(long warehouseId, long zoneId)
    {
        var zoneExists = await ZoneExists(warehouseId, zoneId);

        if (!zoneExists)
        {
            return NotFound(new { message = "Warehouse zone not found." });
        }

        var bins = await _context.StorageBins
            .Where(b => b.ZoneId == zoneId)
            .ToListAsync();

        return Ok(bins);
    }

    // GET: api/warehouses/1/zones/2/bins/1
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetBin(
        long warehouseId, long zoneId, long id)
    {
        var zoneExists = await ZoneExists(warehouseId, zoneId);

        if (!zoneExists)
        {
            return NotFound(new { message = "Warehouse zone not found." });
        }

        var bin = await _context.StorageBins
            .FirstOrDefaultAsync(b =>
                b.ZoneId == zoneId &&
                b.BinId == id);

        if (bin == null)
        {
            return NotFound(new { message = "Storage bin not found." });
        }

        return Ok(bin);
    }

    // POST: api/warehouses/1/zones/2/bins
    [HttpPost]
    public async Task<IActionResult> CreateBin(
        long warehouseId, long zoneId, CreateStorageBinDto dto)
    {
        var zoneExists = await ZoneExists(warehouseId, zoneId);

        if (!zoneExists)
        {
            return NotFound(new { message = "Warehouse zone not found." });
        }

        var binCode = dto.BinCode.Trim();

        var existingBin = await _context.StorageBins
            .AnyAsync(b =>
                b.ZoneId == zoneId &&
                b.BinCode == binCode);

        if (existingBin)
        {
            return Conflict(new
            {
                message = "Bin code already exists in this zone."
            });
        }

        var bin = new StorageBin
        {
            ZoneId = zoneId,
            BinCode = binCode,
            BinType = dto.BinType.Trim(),
            IsActive = true
        };

        _context.StorageBins.Add(bin);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetBin),
            new
            {
                warehouseId = warehouseId,
                zoneId = zoneId,
                id = bin.BinId
            },
            bin
        );
    }

    // PUT: api/warehouses/1/zones/2/bins/1
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateBin(
        long warehouseId,
        long zoneId,
        long id,
        UpdateStorageBinDto dto)
    {
        var zoneExists = await ZoneExists(warehouseId, zoneId);

        if (!zoneExists)
        {
            return NotFound(new { message = "Warehouse zone not found." });
        }

        var bin = await _context.StorageBins
            .FirstOrDefaultAsync(b =>
                b.ZoneId == zoneId &&
                b.BinId == id);

        if (bin == null)
        {
            return NotFound(new { message = "Storage bin not found." });
        }

        bin.BinType = dto.BinType.Trim();
        bin.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return Ok(bin);
    }
}
