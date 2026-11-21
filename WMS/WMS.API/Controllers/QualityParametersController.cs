
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.QualityParameters;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/quality-parameters")]
    [ApiController]
    public class QualityParametersController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public QualityParametersController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/quality-parameters
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var parameters = await _context.QualityParameters
                .AsNoTracking()
                .OrderBy(p => p.ParameterCode)
                .Select(p => new
                {
                    p.ParameterId,
                    p.ParameterCode,
                    p.ParameterName,
                    p.Description,
                    p.UnitOfMeasure
                })
                .ToListAsync();

            return Ok(parameters);
        }

        // GET: api/quality-parameters/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var parameter = await _context.QualityParameters
                .AsNoTracking()
                .Where(p => p.ParameterId == id)
                .Select(p => new
                {
                    p.ParameterId,
                    p.ParameterCode,
                    p.ParameterName,
                    p.Description,
                    p.UnitOfMeasure
                })
                .FirstOrDefaultAsync();

            if (parameter == null)
            {
                return NotFound(new
                {
                    message = "Quality parameter not found."
                });
            }

            return Ok(parameter);
        }

        // POST: api/quality-parameters
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateQualityParameterDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Quality parameter details are required."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.ParameterCode) ||
                string.IsNullOrWhiteSpace(dto.ParameterName))
            {
                return BadRequest(new
                {
                    message = "Parameter code and parameter name are required."
                });
            }

            var code = dto.ParameterCode.Trim();
            var name = dto.ParameterName.Trim();

            if (code.Length > 40)
            {
                return BadRequest(new
                {
                    message = "Parameter code cannot exceed 40 characters."
                });
            }

            if (name.Length > 120)
            {
                return BadRequest(new
                {
                    message = "Parameter name cannot exceed 120 characters."
                });
            }

            var exists = await _context.QualityParameters
                .AnyAsync(p => p.ParameterCode == code);

            if (exists)
            {
                return Conflict(new
                {
                    message = "Quality parameter code already exists."
                });
            }

            var parameter = new QualityParameter
            {
                ParameterCode = code,
                ParameterName = name,
                Description = string.IsNullOrWhiteSpace(dto.Description)
                    ? null
                    : dto.Description.Trim(),
                UnitOfMeasure = string.IsNullOrWhiteSpace(dto.UnitOfMeasure)
                    ? null
                    : dto.UnitOfMeasure.Trim()
            };

            _context.QualityParameters.Add(parameter);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = parameter.ParameterId },
                new
                {
                    message = "Quality parameter created successfully.",
                    parameter.ParameterId,
                    parameter.ParameterCode,
                    parameter.ParameterName,
                    parameter.Description,
                    parameter.UnitOfMeasure
                });
        }

        // PUT: api/quality-parameters/1
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] UpdateQualityParameterDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Quality parameter details are required."
                });
            }

            var parameter = await _context.QualityParameters
                .FirstOrDefaultAsync(p => p.ParameterId == id);

            if (parameter == null)
            {
                return NotFound(new
                {
                    message = "Quality parameter not found."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.ParameterCode) ||
                string.IsNullOrWhiteSpace(dto.ParameterName))
            {
                return BadRequest(new
                {
                    message = "Parameter code and parameter name are required."
                });
            }

            var code = dto.ParameterCode.Trim();
            var name = dto.ParameterName.Trim();

            if (code.Length > 40)
            {
                return BadRequest(new
                {
                    message = "Parameter code cannot exceed 40 characters."
                });
            }

            if (name.Length > 120)
            {
                return BadRequest(new
                {
                    message = "Parameter name cannot exceed 120 characters."
                });
            }

            var duplicate = await _context.QualityParameters
                .AnyAsync(p =>
                    p.ParameterCode == code &&
                    p.ParameterId != id);

            if (duplicate)
            {
                return Conflict(new
                {
                    message = "Another quality parameter already uses this code."
                });
            }

            parameter.ParameterCode = code;
            parameter.ParameterName = name;
            parameter.Description =
                string.IsNullOrWhiteSpace(dto.Description)
                    ? null
                    : dto.Description.Trim();
            parameter.UnitOfMeasure =
                string.IsNullOrWhiteSpace(dto.UnitOfMeasure)
                    ? null
                    : dto.UnitOfMeasure.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Quality parameter updated successfully.",
                parameter.ParameterId,
                parameter.ParameterCode,
                parameter.ParameterName,
                parameter.Description,
                parameter.UnitOfMeasure
            });
        }
    }
}
