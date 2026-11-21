
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs;
using WMS.API.Models;

namespace WMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehousesController : ControllerBase
{
    private readonly WmsDbContext _context;

    public WarehousesController(WmsDbContext context)
    {
        _context = context;
    }

    // GET: api/warehouses
    [HttpGet]
    public async Task<IActionResult> GetWarehouses()
    {
        var warehouses = await _context.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.WarehouseCode)
            .ToListAsync();

        return Ok(warehouses);
    }

    // GET: api/warehouses/1
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetWarehouse(long id)
    {
        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WarehouseId == id);

        if (warehouse == null)
        {
            return NotFound(new
            {
                message = "Warehouse not found."
            });
        }

        return Ok(warehouse);
    }

    // POST: api/warehouses
    [HttpPost]
    public async Task<IActionResult> CreateWarehouse(
        [FromBody] CreateWarehouseDto dto)
    {
        if (dto == null)
        {
            return BadRequest(new
            {
                message = "Warehouse data is required."
            });
        }

        var warehouseCode = dto.WarehouseCode?.Trim();
        var warehouseName = dto.WarehouseName?.Trim();

        if (string.IsNullOrWhiteSpace(warehouseCode) ||
            string.IsNullOrWhiteSpace(warehouseName))
        {
            return BadRequest(new
            {
                message = "Warehouse code and warehouse name are required."
            });
        }

        if (warehouseCode.Length > 30 ||
            warehouseName.Length > 150)
        {
            return BadRequest(new
            {
                message = "Warehouse code must not exceed 30 characters and warehouse name must not exceed 150 characters."
            });
        }

        if (dto.City?.Length > 100)
        {
            return BadRequest(new
            {
                message = "City must not exceed 100 characters."
            });
        }

        var existingWarehouse = await _context.Warehouses
            .AnyAsync(w => w.WarehouseCode == warehouseCode);

        if (existingWarehouse)
        {
            return Conflict(new
            {
                message = "Warehouse code already exists."
            });
        }

        var warehouse = new Warehouse
        {
            WarehouseCode = warehouseCode,
            WarehouseName = warehouseName,
            City = dto.City?.Trim(),
            Address = dto.Address?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Warehouses.Add(warehouse);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Unable to create warehouse. The warehouse code may already exist."
            });
        }

        return CreatedAtAction(
            nameof(GetWarehouse),
            new { id = warehouse.WarehouseId },
            warehouse);
    }

    // PUT: api/warehouses/1
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateWarehouse(
        long id,
        [FromBody] UpdateWarehouseDto dto)
    {
        if (dto == null)
        {
            return BadRequest(new
            {
                message = "Warehouse data is required."
            });
        }

        var warehouseName = dto.WarehouseName?.Trim();

        if (string.IsNullOrWhiteSpace(warehouseName))
        {
            return BadRequest(new
            {
                message = "Warehouse name is required."
            });
        }

        if (warehouseName.Length > 150)
        {
            return BadRequest(new
            {
                message = "Warehouse name must not exceed 150 characters."
            });
        }

        if (dto.City?.Length > 100)
        {
            return BadRequest(new
            {
                message = "City must not exceed 100 characters."
            });
        }

        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseId == id);

        if (warehouse == null)
        {
            return NotFound(new
            {
                message = "Warehouse not found."
            });
        }

        warehouse.WarehouseName = warehouseName;
        warehouse.City = dto.City?.Trim();
        warehouse.Address = dto.Address?.Trim();

        await _context.SaveChangesAsync();

        return Ok(warehouse);
    }

    // PATCH: api/warehouses/1/deactivate
    [HttpPatch("{id:long}/deactivate")]
    public async Task<IActionResult> DeactivateWarehouse(long id)
    {
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseId == id);

        if (warehouse == null)
        {
            return NotFound(new
            {
                message = "Warehouse not found."
            });
        }

        if (!warehouse.IsActive)
        {
            return Ok(new
            {
                message = "Warehouse is already deactivated."
            });
        }

        warehouse.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Warehouse deactivated successfully."
        });
    }
}
