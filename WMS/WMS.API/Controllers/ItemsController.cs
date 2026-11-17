
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.Items;
using WMS.API.Models;

namespace WMS.API.Controllers;

[Route("api/items")]
[ApiController]
public class ItemsController : ControllerBase
{
    private readonly WmsDbContext _context;

    public ItemsController(WmsDbContext context)
    {
        _context = context;
    }

    // GET: api/items
    [HttpGet]
    public async Task<IActionResult> GetItems()
    {
        var items = await _context.Items
            .Include(i => i.Category)
            .Include(i => i.BaseUom)
            .OrderBy(i => i.ItemCode)
            .Select(i => new
            {
                i.ItemId,
                i.ItemCode,
                i.ItemName,
                i.Description,
                i.CategoryId,
                CategoryName = i.Category.CategoryName,
                i.BaseUomId,
                UomCode = i.BaseUom.UomCode,
                UomName = i.BaseUom.UomName,
                i.StockRotationMethod,
                i.RequiresExpiry,
                i.RequiresQualityInspection,
                i.ReorderLevel,
                i.IsActive,
                i.CreatedAt,
                i.UpdatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: api/items/1
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetItem(long id)
    {
        var item = await _context.Items
            .Include(i => i.Category)
            .Include(i => i.BaseUom)
            .Where(i => i.ItemId == id)
            .Select(i => new
            {
                i.ItemId,
                i.ItemCode,
                i.ItemName,
                i.Description,
                i.CategoryId,
                CategoryName = i.Category.CategoryName,
                i.BaseUomId,
                UomCode = i.BaseUom.UomCode,
                UomName = i.BaseUom.UomName,
                i.StockRotationMethod,
                i.RequiresExpiry,
                i.RequiresQualityInspection,
                i.ReorderLevel,
                i.IsActive,
                i.CreatedAt,
                i.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (item == null)
        {
            return NotFound("Item not found.");
        }

        return Ok(item);
    }

    // POST: api/items
    [HttpPost]
    public async Task<IActionResult> CreateItem(CreateItemDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ItemCode) ||
            string.IsNullOrWhiteSpace(dto.ItemName))
        {
            return BadRequest("Item code and item name are required.");
        }

        if (dto.ReorderLevel < 0)
        {
            return BadRequest("Reorder level cannot be negative.");
        }

        string rotationMethod = dto.StockRotationMethod.Trim().ToUpper();

        if (rotationMethod != "FIFO" && rotationMethod != "FEFO")
        {
            return BadRequest("Stock rotation method must be FIFO or FEFO.");
        }

        string itemCode = dto.ItemCode.Trim();

        bool codeExists = await _context.Items
            .AnyAsync(i => i.ItemCode == itemCode);

        if (codeExists)
        {
            return Conflict("Item code already exists.");
        }

        bool categoryExists = await _context.ItemCategories
            .AnyAsync(c => c.CategoryId == dto.CategoryId && c.IsActive);

        if (!categoryExists)
        {
            return BadRequest("Selected category does not exist or is inactive.");
        }

        bool uomExists = await _context.UnitsOfMeasures
            .AnyAsync(u => u.UomId == dto.BaseUomId && u.IsActive);

        if (!uomExists)
        {
            return BadRequest("Selected unit of measure does not exist or is inactive.");
        }

        var item = new Item
        {
            ItemCode = itemCode,
            ItemName = dto.ItemName.Trim(),
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            BaseUomId = dto.BaseUomId,
            StockRotationMethod = rotationMethod,
            RequiresExpiry = dto.RequiresExpiry,
            RequiresQualityInspection = dto.RequiresQualityInspection,
            ReorderLevel = dto.ReorderLevel,
            IsActive = true
        };

        _context.Items.Add(item);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetItem),
            new { id = item.ItemId },
            item
        );
    }

    // PUT: api/items/1
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateItem(long id, UpdateItemDto dto)
    {
        var item = await _context.Items.FindAsync(id);

        if (item == null)
        {
            return NotFound("Item not found.");
        }

        if (string.IsNullOrWhiteSpace(dto.ItemName))
        {
            return BadRequest("Item name is required.");
        }

        if (dto.ReorderLevel < 0)
        {
            return BadRequest("Reorder level cannot be negative.");
        }

        string rotationMethod = dto.StockRotationMethod.Trim().ToUpper();

        if (rotationMethod != "FIFO" && rotationMethod != "FEFO")
        {
            return BadRequest("Stock rotation method must be FIFO or FEFO.");
        }

        bool categoryExists = await _context.ItemCategories
            .AnyAsync(c => c.CategoryId == dto.CategoryId && c.IsActive);

        if (!categoryExists)
        {
            return BadRequest("Selected category does not exist or is inactive.");
        }

        bool uomExists = await _context.UnitsOfMeasures
            .AnyAsync(u => u.UomId == dto.BaseUomId && u.IsActive);

        if (!uomExists)
        {
            return BadRequest("Selected unit of measure does not exist or is inactive.");
        }

        item.ItemName = dto.ItemName.Trim();
        item.Description = dto.Description;
        item.CategoryId = dto.CategoryId;
        item.BaseUomId = dto.BaseUomId;
        item.StockRotationMethod = rotationMethod;
        item.RequiresExpiry = dto.RequiresExpiry;
        item.RequiresQualityInspection = dto.RequiresQualityInspection;
        item.ReorderLevel = dto.ReorderLevel;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok("Item updated successfully.");
    }

    // PATCH: api/items/1/deactivate
    [HttpPatch("{id:long}/deactivate")]
    public async Task<IActionResult> DeactivateItem(long id)
    {
        var item = await _context.Items.FindAsync(id);

        if (item == null)
        {
            return NotFound("Item not found.");
        }

        if (!item.IsActive)
        {
            return BadRequest("Item is already inactive.");
        }

        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok("Item deactivated successfully.");
    }
}
