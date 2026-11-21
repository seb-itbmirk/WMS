
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;

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
    }
}
