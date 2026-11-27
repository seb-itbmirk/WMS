
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.Suppliers;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/suppliers")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public SuppliersController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/suppliers
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .OrderBy(s => s.SupplierName)
                .Select(s => new
                {
                    s.SupplierId,
                    s.SupplierCode,
                    s.SupplierName,
                    s.ContactPerson,
                    s.Phone,
                    s.Email,
                    s.Address,
                    s.IsActive,
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(suppliers);
        }

        // GET: api/suppliers/active
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.SupplierName)
                .Select(s => new
                {
                    s.SupplierId,
                    s.SupplierCode,
                    s.SupplierName,
                    s.ContactPerson,
                    s.Phone,
                    s.Email,
                    s.Address,
                    s.IsActive
                })
                .ToListAsync();

            return Ok(suppliers);
        }

        // GET: api/suppliers/5
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.SupplierId == id)
                .Select(s => new
                {
                    s.SupplierId,
                    s.SupplierCode,
                    s.SupplierName,
                    s.ContactPerson,
                    s.Phone,
                    s.Email,
                    s.Address,
                    s.IsActive,
                    s.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (supplier == null)
            {
                return NotFound(new
                {
                    message = $"Supplier with ID {id} was not found."
                });
            }

            return Ok(supplier);
        }

        // POST: api/suppliers
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateSupplierDto dto)
        {
            var supplierCode = dto.SupplierCode.Trim();

            var duplicateCode = await _context.Suppliers
                .AnyAsync(s => s.SupplierCode.ToLower() == supplierCode.ToLower());

            if (duplicateCode)
            {
                return Conflict(new
                {
                    message = "Supplier code already exists."
                });
            }

            var supplier = new Supplier
            {
                SupplierCode = supplierCode,
                SupplierName = dto.SupplierName.Trim(),
                ContactPerson = dto.ContactPerson?.Trim(),
                Phone = dto.Phone?.Trim(),
                Email = dto.Email?.Trim(),
                Address = dto.Address?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Suppliers.Add(supplier);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = supplier.SupplierId },
                new
                {
                    supplier.SupplierId,
                    supplier.SupplierCode,
                    supplier.SupplierName,
                    supplier.ContactPerson,
                    supplier.Phone,
                    supplier.Email,
                    supplier.Address,
                    supplier.IsActive,
                    supplier.CreatedAt
                });
        }

        // PUT: api/suppliers/5
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] UpdateSupplierDto dto)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierId == id);

            if (supplier == null)
            {
                return NotFound(new
                {
                    message = $"Supplier with ID {id} was not found."
                });
            }

            var supplierCode = dto.SupplierCode.Trim();

            var duplicateCode = await _context.Suppliers
                .AnyAsync(s =>
                    s.SupplierId != id &&
                    s.SupplierCode.ToLower() == supplierCode.ToLower());

            if (duplicateCode)
            {
                return Conflict(new
                {
                    message = "Supplier code already exists."
                });
            }

            supplier.SupplierCode = supplierCode;
            supplier.SupplierName = dto.SupplierName.Trim();
            supplier.ContactPerson = dto.ContactPerson?.Trim();
            supplier.Phone = dto.Phone?.Trim();
            supplier.Email = dto.Email?.Trim();
            supplier.Address = dto.Address?.Trim();
            supplier.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Supplier updated successfully.",
                supplier.SupplierId,
                supplier.SupplierCode,
                supplier.SupplierName,
                supplier.ContactPerson,
                supplier.Phone,
                supplier.Email,
                supplier.Address,
                supplier.IsActive,
                supplier.CreatedAt
            });
        }
    }
}
