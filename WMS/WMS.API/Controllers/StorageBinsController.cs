
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs;
using WMS.API.Models;

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

    private async Task<bool> ZoneExists(long warehouseId, long zoneId)
    {
        return await _context.WarehouseZones
            .AsNoTracking()
            .AnyAsync(z =>
                z.WarehouseId == warehouseId &&
                z.ZoneId == zoneId);
    }

    // GET: api/warehouses/1/zones/2/bins
    [HttpGet]
    public async Task<IActionResult> GetBins(
        long warehouseId,
        long zoneId)
    {
        if (warehouseId <= 0 || zoneId <= 0)
        {
            return BadRequest(new
            {
                message = "Valid warehouse and zone IDs are required."
            });
        }

        var zoneExists = await ZoneExists(warehouseId, zoneId);

        if (!zoneExists)
        {
            return NotFound(new
            {
                message = "Warehouse zone not found."
            });
        }

        var bins = await _context.StorageBins
            .AsNoTracking()
            .Where(b => b.ZoneId == zoneId)
            .OrderBy(b => b.BinCode)
            .ToListAsync();

        return Ok(bins);
    }

    // GET: api/warehouses/1/zones/2/bins/1
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetBin(
        long warehouseId,
        long zoneId,
        long id)
    {
        if (warehouseId <= 0 || zoneId <= 0 || id <= 0)
        {
            return BadRequest(new
            {
                message = "Valid warehouse, zone, and bin IDs are required."
            });
        }

        var zoneExists = await ZoneExists(warehouseId, zoneId);

        if (!zoneExists)
        {
            return NotFound(new
            {
                message = "Warehouse zone not found."
            });
        }

        var bin = await _context.StorageBins
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.ZoneId == zoneId &&
                b.BinId == id);

        if (bin == null)
        {
            return NotFound(new
            {
                message = "Storage bin not found."
            });
        }

        return Ok(bin);
    }

    // POST: api/warehouses/1/zones/2/bins
    [HttpPost]
    public async Task<IActionResult> CreateBin(
        long warehouseId,
        long zoneId,
        [FromBody] CreateStorageBinDto dto)
    {
        if (warehouseId <= 0 || zoneId <= 0)
        {
            return BadRequest(new
            {
                message = "Valid warehouse and zone IDs are required."
            });
        }

        if (dto == null)
        {
            return BadRequest(new
            {
                message = "Storage bin details are required."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.BinCode))
        {
            return BadRequest(new
            {
                message = "Bin code is required."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.BinType))
        {
            return BadRequest(new
            {
                message = "Bin type is required."
            });
        }

        var binCode = dto.BinCode.Trim();
        var binType = dto.BinType.Trim();

        if (binCode.Length > 50)
        {
            return BadRequest(new
            {
                message = "Bin code cannot exceed 50 characters."
            });
        }

        if (binType.Length > 30)
        {
            return BadRequest(new
            {
                message = "Bin type cannot exceed 30 characters."
            });
        }

        var zoneExists = await ZoneExists(warehouseId, zoneId);

        if (!zoneExists)
        {
            return NotFound(new
            {
                message = "Warehouse zone not found."
            });
        }

        var existingBin = await _context.StorageBins
            .AsNoTracking()
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
            BinType = binType,
            IsActive = true
        };

        _context.StorageBins.Add(bin);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Unable to create storage bin. The bin code may already exist in this zone."
            });
        }

        return CreatedAtAction(
            nameof(GetBin),
            new
            {
                warehouseId,
                zoneId,
                id = bin.BinId
            },
            bin);
    }

    // PUT: api/warehouses/1/zones/2/bins/1
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateBin(
        long warehouseId,
        long zoneId,
        long id,
        [FromBody] UpdateStorageBinDto dto)
    {
        if (warehouseId <= 0 || zoneId <= 0 || id <= 0)
        {
            return BadRequest(new
            {
                message = "Valid warehouse, zone, and bin IDs are required."
            });
        }

        if (dto == null)
        {
            return BadRequest(new
            {
                message = "Storage bin details are required."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.BinType))
        {
            return BadRequest(new
            {
                message = "Bin type is required."
            });
        }

        var binType = dto.BinType.Trim();

        if (binType.Length > 30)
        {
            return BadRequest(new
            {
                message = "Bin type cannot exceed 30 characters."
            });
        }

        var zoneExists = await ZoneExists(warehouseId, zoneId);

        if (!zoneExists)
        {
            return NotFound(new
            {
                message = "Warehouse zone not found."
            });
        }

        var bin = await _context.StorageBins
            .FirstOrDefaultAsync(b =>
                b.ZoneId == zoneId &&
                b.BinId == id);

        if (bin == null)
        {
            return NotFound(new
            {
                message = "Storage bin not found."
            });
        }

        bin.BinType = binType;
        bin.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return Ok(bin);
    }
}
