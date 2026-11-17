using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;
using WMS.API.DTOs;

namespace WMS.API.Controllers;

[Route("api/item-categories")]
[ApiController]
public class ItemCategoriesController : ControllerBase
{
    private readonly WmsDbContext _context;

    public ItemCategoriesController(WmsDbContext context)
    {
        _context = context;
    }

    // GET: api/item-categories
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.ItemCategories
            .OrderBy(c => c.CategoryCode)
            .ToListAsync();

        return Ok(categories);
    }

    // GET: api/item-categories/1
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetCategory(long id)
    {
        var category = await _context.ItemCategories
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Item category not found."
            });
        }

        return Ok(category);
    }

    // POST: api/item-categories
    [HttpPost]
    public async Task<IActionResult> CreateCategory(
        CreateItemCategoryDto dto)
    {
        var categoryCode = dto.CategoryCode.Trim();

        var existingCategory = await _context.ItemCategories
            .AnyAsync(c => c.CategoryCode == categoryCode);

        if (existingCategory)
        {
            return Conflict(new
            {
                message = "Category code already exists."
            });
        }

        var category = new ItemCategory
        {
            CategoryCode = categoryCode,
            CategoryName = dto.CategoryName.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = true
        };

        _context.ItemCategories.Add(category);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.CategoryId },
            category
        );
    }

    // PUT: api/item-categories/1
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateCategory(
        long id,
        UpdateItemCategoryDto dto)
    {
        var category = await _context.ItemCategories
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Item category not found."
            });
        }

        category.CategoryName = dto.CategoryName.Trim();
        category.Description = dto.Description?.Trim();

        await _context.SaveChangesAsync();

        return Ok(category);
    }

    // PATCH: api/item-categories/1/deactivate
    [HttpPatch("{id:long}/deactivate")]
    public async Task<IActionResult> DeactivateCategory(long id)
    {
        var category = await _context.ItemCategories
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Item category not found."
            });
        }

        category.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Item category deactivated successfully."
        });
    }
}
