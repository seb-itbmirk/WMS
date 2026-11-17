using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;
using WMS.API.DTOs;

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
        var warehouses = await _context.Warehouses.ToListAsync();

        return Ok(warehouses);
    }

    // GET: api/warehouses/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWarehouse(long id)
    {
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseId == id);

        if (warehouse == null)
        {
            return NotFound(new { message = "Warehouse not found." });
        }

        return Ok(warehouse);
    }

    // POST: api/warehouses
    [HttpPost]
    public async Task<IActionResult> CreateWarehouse(
        CreateWarehouseDto dto)
    {
        var existingWarehouse = await _context.Warehouses
            .AnyAsync(w => w.WarehouseCode == dto.WarehouseCode);

        if (existingWarehouse)
        {
            return Conflict(new
            {
                message = "Warehouse code already exists."
            });
        }

        var warehouse = new Warehouse
        {
            WarehouseCode = dto.WarehouseCode.Trim(),
            WarehouseName = dto.WarehouseName.Trim(),
            City = dto.City,
            Address = dto.Address,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Warehouses.Add(warehouse);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetWarehouse),
            new { id = warehouse.WarehouseId },
            warehouse
        );
    }

// PUT: api/warehouses/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWarehouse(
        long id, UpdateWarehouseDto dto)
    {
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseId == id);

        if (warehouse == null)
        {
            return NotFound(new { message = "Warehouse not found." });
        }

        warehouse.WarehouseName = dto.WarehouseName.Trim();
        warehouse.City = dto.City;
        warehouse.Address = dto.Address;

        await _context.SaveChangesAsync();

        return Ok(warehouse);
    }

    // PATCH: api/warehouses/1/deactivate
    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> DeactivateWarehouse(long id)
    {
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseId == id);

        if (warehouse == null)
        {
            return NotFound(new { message = "Warehouse not found." });
        }

        warehouse.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Warehouse deactivated successfully."
        });
    }
}