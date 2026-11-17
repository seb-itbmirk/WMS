
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;
using WMS.API.DTOs.ItemBarcodes;

namespace WMS.API.Controllers
{
    [Route("api/item-barcodes")]
    [ApiController]
    public class ItemBarcodesController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public ItemBarcodesController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/item-barcodes
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var barcodes = await _context.ItemBarcodes
                .Include(b => b.Item)
                .OrderBy(b => b.BarcodeValue)
                .Select(b => new
                {
                    b.BarcodeId,
                    b.ItemId,
                    b.BarcodeValue,
                    b.BarcodeType,
                    b.IsPrimary,
                    b.IsActive,
                    ItemCode = b.Item.ItemCode,
                    ItemName = b.Item.ItemName
                })
                .ToListAsync();

            return Ok(barcodes);
        }

        // GET: api/item-barcodes/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var barcode = await _context.ItemBarcodes
                .Include(b => b.Item)
                .Where(b => b.BarcodeId == id)
                .Select(b => new
                {
                    b.BarcodeId,
                    b.ItemId,
                    b.BarcodeValue,
                    b.BarcodeType,
                    b.IsPrimary,
                    b.IsActive,
                    ItemCode = b.Item.ItemCode,
                    ItemName = b.Item.ItemName
                })
                .FirstOrDefaultAsync();

            if (barcode == null)
                return NotFound("Barcode not found.");

            return Ok(barcode);
        }

        // GET: api/item-barcodes/scan/4800000000012
        [HttpGet("scan/{barcode}")]
        public async Task<IActionResult> ScanBarcode(string barcode)
        {
            var result = await _context.ItemBarcodes
                .Where(b => b.BarcodeValue == barcode && b.IsActive)
                .Select(b => new
                {
                    b.BarcodeId,
                    b.BarcodeValue,
                    b.BarcodeType,
                    b.ItemId,
                    ItemCode = b.Item.ItemCode,
                    ItemName = b.Item.ItemName,
                    b.Item.IsActive
                })
                .FirstOrDefaultAsync();

            if (result == null || !result.IsActive)
                return NotFound("Active barcode or item not found.");

            return Ok(result);
        }

        // POST: api/item-barcodes
        [HttpPost]
        public async Task<IActionResult> Create(CreateItemBarcodeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.BarcodeValue))
                return BadRequest("Barcode value is required.");

            var itemExists = await _context.Items
                .AnyAsync(i => i.ItemId == dto.ItemId && i.IsActive);

            if (!itemExists)
                return BadRequest("Selected item does not exist or is inactive.");

            var barcodeExists = await _context.ItemBarcodes
                .AnyAsync(b => b.BarcodeValue == dto.BarcodeValue);

            if (barcodeExists)
                return Conflict("Barcode value already exists.");

            if (dto.IsPrimary)
            {
                var primaryExists = await _context.ItemBarcodes
                    .AnyAsync(b => b.ItemId == dto.ItemId
                                && b.IsPrimary
                                && b.IsActive);

                if (primaryExists)
                    return Conflict("This item already has an active primary barcode.");
            }

            var barcode = new ItemBarcode
            {
                ItemId = dto.ItemId,
                BarcodeValue = dto.BarcodeValue,
                BarcodeType = dto.BarcodeType,
                IsPrimary = dto.IsPrimary,
                IsActive = true
            };

            _context.ItemBarcodes.Add(barcode);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Barcode registered successfully.",
                barcode.BarcodeId,
                barcode.ItemId,
                barcode.BarcodeValue
            });
        }

        // PATCH: api/item-barcodes/1/deactivate
        [HttpPatch("{id:long}/deactivate")]
        public async Task<IActionResult> Deactivate(long id)
        {
            var barcode = await _context.ItemBarcodes
                .FirstOrDefaultAsync(b => b.BarcodeId == id);

            if (barcode == null)
                return NotFound("Barcode not found.");

            barcode.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Barcode deactivated successfully."
            });
        }
    }
}
