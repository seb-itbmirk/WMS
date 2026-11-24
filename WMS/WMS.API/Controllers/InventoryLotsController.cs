
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;
using WMS.API.DTOs.InventoryLots;

namespace WMS.API.Controllers
{
    [Route("api/inventory-lots")]
    [ApiController]
    public class InventoryLotsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public InventoryLotsController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/inventory-lots
        [Authorize(Policy = "Permission:inventory.view")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lots = await _context.InventoryLots
                .Include(l => l.Item)
                .OrderBy(l => l.BatchCode)
                .Select(l => new
                {
                    l.LotId,
                    l.ItemId,
                    ItemCode = l.Item.ItemCode,
                    ItemName = l.Item.ItemName,
                    l.BatchCode,
                    l.SupplierBatchCode,
                    l.ManufactureDate,
                    l.ExpiryDate,
                    l.ReceivedDate,
                    l.QualityStatus,
                    l.Notes,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(lots);
        }

        // GET: api/inventory-lots/1
        [Authorize(Policy = "Permission:inventory.view")]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var lot = await _context.InventoryLots
                .Include(l => l.Item)
                .Where(l => l.LotId == id)
                .Select(l => new
                {
                    l.LotId,
                    l.ItemId,
                    ItemCode = l.Item.ItemCode,
                    ItemName = l.Item.ItemName,
                    l.BatchCode,
                    l.SupplierBatchCode,
                    l.ManufactureDate,
                    l.ExpiryDate,
                    l.ReceivedDate,
                    l.QualityStatus,
                    l.Notes,
                    l.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (lot == null)
                return NotFound("Inventory lot not found.");

            return Ok(lot);
        }

        // GET: api/inventory-lots/item/1
        [Authorize(Policy = "Permission:inventory.view")]
        [HttpGet("item/{itemId:long}")]
        public async Task<IActionResult> GetByItem(long itemId)
        {
            var itemExists = await _context.Items
                .AnyAsync(i => i.ItemId == itemId);

            if (!itemExists)
                return NotFound("Item not found.");

            var lots = await _context.InventoryLots
                .Where(l => l.ItemId == itemId)
                .OrderBy(l => l.ReceivedDate)
                .Select(l => new
                {
                    l.LotId,
                    l.ItemId,
                    l.BatchCode,
                    l.SupplierBatchCode,
                    l.ManufactureDate,
                    l.ExpiryDate,
                    l.ReceivedDate,
                    l.QualityStatus,
                    l.Notes
                })
                .ToListAsync();

            return Ok(lots);
        }

        // POST: api/inventory-lots
        [Authorize(Policy = "Permission:inventory.manage")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateInventoryLotDto dto)
        {
            // Validate batch code
            if (string.IsNullOrWhiteSpace(dto.BatchCode))
                return BadRequest("Batch code is required.");

            // Validate quality status
            if (string.IsNullOrWhiteSpace(dto.QualityStatus))
                return BadRequest("Quality status is required.");

            var qualityStatus = dto.QualityStatus.Trim().ToUpper();

            var validStatuses = new[]
            {
                "PENDING",
                "APPROVED",
                "REJECTED",
                "QUARANTINED"
            };

            if (!validStatuses.Contains(qualityStatus))
            {
                return BadRequest(
                    "Invalid quality status. Allowed values: PENDING, APPROVED, REJECTED, QUARANTINED.");
            }

            // Validate expiry date
            if (dto.ExpiryDate.HasValue &&
                dto.ManufactureDate.HasValue &&
                dto.ExpiryDate.Value < dto.ManufactureDate.Value)
            {
                return BadRequest(
                    "Expiry date cannot be earlier than manufacture date.");
            }

            // Check if item exists and is active
            var itemExists = await _context.Items
                .AnyAsync(i => i.ItemId == dto.ItemId && i.IsActive);

            if (!itemExists)
                return BadRequest("Selected item does not exist or is inactive.");

            // Check duplicate batch code for the same item
            var batchCode = dto.BatchCode.Trim();

            var batchExists = await _context.InventoryLots
                .AnyAsync(l => l.ItemId == dto.ItemId &&
                               l.BatchCode == batchCode);

            if (batchExists)
                return Conflict("This batch code already exists for the selected item.");

            // Create inventory lot
            var lot = new InventoryLot
            {
                ItemId = dto.ItemId,
                BatchCode = batchCode,
                SupplierBatchCode = dto.SupplierBatchCode,
                ManufactureDate = dto.ManufactureDate,
                ExpiryDate = dto.ExpiryDate,
                ReceivedDate = dto.ReceivedDate,
                QualityStatus = qualityStatus,
                Notes = dto.Notes
            };

            _context.InventoryLots.Add(lot);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("DATABASE ERROR:");
                Console.WriteLine(ex.InnerException?.Message);

                return StatusCode(500,
                    "Unable to register inventory lot. Check the database constraints and server logs.");
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = lot.LotId },
                new
                {
                    message = "Inventory lot registered successfully.",
                    lot.LotId,
                    lot.ItemId,
                    lot.BatchCode,
                    lot.QualityStatus
                });
        }
    }
}
