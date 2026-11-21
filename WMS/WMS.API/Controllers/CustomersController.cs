using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.Customers;
using WMS.API.Models;

namespace WMS.API.Controllers;

[Route("api/customers")]
[ApiController]
public class CustomersController : ControllerBase
{
    private readonly WmsDbContext _context;

    public CustomersController(WmsDbContext context)
    {
        _context = context;
    }

    // GET: api/customers
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .OrderBy(c => c.CustomerName)
            .Select(c => new
            {
                c.CustomerId,
                c.CustomerCode,
                c.CustomerName,
                c.ContactPerson,
                c.Phone,
                c.Email,
                c.Address,
                c.IsActive,
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(customers);
    }

    // GET: api/customers/active
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CustomerName)
            .Select(c => new
            {
                c.CustomerId,
                c.CustomerCode,
                c.CustomerName,
                c.ContactPerson,
                c.Phone,
                c.Email,
                c.Address,
                c.IsActive
            })
            .ToListAsync();

        return Ok(customers);
    }

    // GET: api/customers/1
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null)
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        return Ok(customer);
    }

    // POST: api/customers
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerDto dto)
    {
        var customerCode = dto.CustomerCode.Trim();

        var codeExists = await _context.Customers
            .AnyAsync(c => c.CustomerCode == customerCode);

        if (codeExists)
        {
            return Conflict(new
            {
                message = "Customer code already exists."
            });
        }

        var customer = new Customer
        {
            CustomerCode = customerCode,
            CustomerName = dto.CustomerName.Trim(),
            ContactPerson = NormalizeOptionalValue(dto.ContactPerson),
            Phone = NormalizeOptionalValue(dto.Phone),
            Email = NormalizeOptionalValue(dto.Email),
            Address = NormalizeOptionalValue(dto.Address),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = customer.CustomerId },
            customer
        );
    }

    // PUT: api/customers/1
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        UpdateCustomerDto dto)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null)
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        customer.CustomerName = dto.CustomerName.Trim();
        customer.ContactPerson = NormalizeOptionalValue(dto.ContactPerson);
        customer.Phone = NormalizeOptionalValue(dto.Phone);
        customer.Email = NormalizeOptionalValue(dto.Email);
        customer.Address = NormalizeOptionalValue(dto.Address);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Customer updated successfully.",
            customer
        });
    }

    // PATCH: api/customers/1/deactivate
    [HttpPatch("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null)
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        if (!customer.IsActive)
        {
            return BadRequest(new
            {
                message = "Customer is already inactive."
            });
        }

        customer.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Customer deactivated successfully."
        });
    }

    // PATCH: api/customers/1/activate
    [HttpPatch("{id:long}/activate")]
    public async Task<IActionResult> Activate(long id)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null)
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        if (customer.IsActive)
        {
            return BadRequest(new
            {
                message = "Customer is already active."
            });
        }

        customer.IsActive = true;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Customer activated successfully."
        });
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}