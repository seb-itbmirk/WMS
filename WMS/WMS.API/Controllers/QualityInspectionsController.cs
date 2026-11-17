
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.QualityInspections;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/quality-inspections")]
    [ApiController]
    public class QualityInspectionsController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public QualityInspectionsController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/quality-inspections
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var inspections = await _context.QualityInspections
                .OrderByDescending(i => i.InspectionDate)
                .Select(i => new
                {
                    i.InspectionId,
                    i.LotId,
                    i.InspectionType,
                    i.InspectionDate,
                    i.OverallResult,
                    i.ReceiptId,
                    i.ProductionOrderId,
                    i.InspectedBy,
                    i.Remarks,
                    LotBatchCode = i.Lot.BatchCode,
                    ItemCode = i.Lot.Item.ItemCode,
                    ItemName = i.Lot.Item.ItemName
                })
                .ToListAsync();

            return Ok(inspections);
        }

        // GET: api/quality-inspections/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var inspection = await _context.QualityInspections
                .Where(i => i.InspectionId == id)
                .Select(i => new
                {
                    i.InspectionId,
                    i.LotId,
                    i.InspectionType,
                    i.InspectionDate,
                    i.OverallResult,
                    i.ReceiptId,
                    i.ProductionOrderId,
                    i.InspectedBy,
                    i.Remarks,
                    LotBatchCode = i.Lot.BatchCode,
                    ItemCode = i.Lot.Item.ItemCode,
                    ItemName = i.Lot.Item.ItemName,

                    Lines = i.QualityInspectionLines
                        .OrderBy(l => l.InspectionLineId)
                        .Select(l => new
                        {
                            l.InspectionLineId,
                            l.ParameterId,
                            ParameterCode = l.Parameter.ParameterCode,
                            ParameterName = l.Parameter.ParameterName,
                            l.Parameter.UnitOfMeasure,
                            l.MeasuredValue,
                            l.MinimumValue,
                            l.MaximumValue,
                            l.Result,
                            l.Remarks
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (inspection == null)
                return NotFound(new { message = "Quality inspection not found." });

            return Ok(inspection);
        }

        // POST: api/quality-inspections
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateQualityInspectionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.InspectionType))
                return BadRequest(new { message = "Inspection type is required." });

            var inspectionType = dto.InspectionType.Trim().ToUpper();

            if (!new[] { "RECEIVING", "IN_PROCESS", "FINAL" }
                .Contains(inspectionType))
            {
                return BadRequest(new
                {
                    message = "Inspection type must be RECEIVING, IN_PROCESS, or FINAL."
                });
            }

            var lot = await _context.InventoryLots
                .FirstOrDefaultAsync(l => l.LotId == dto.LotId);

            if (lot == null)
                return NotFound(new { message = "Inventory lot not found." });

            if (dto.ReceiptId.HasValue &&
                !await _context.GoodsReceipts.AnyAsync(
                    r => r.ReceiptId == dto.ReceiptId.Value))
            {
                return BadRequest(new { message = "Goods receipt not found." });
            }

            if (dto.ProductionOrderId.HasValue &&
                !await _context.ProductionOrders.AnyAsync(
                    p => p.ProductionOrderId == dto.ProductionOrderId.Value))
            {
                return BadRequest(new { message = "Production order not found." });
            }

            if (dto.InspectedBy.HasValue &&
                !await _context.AppUsers.AnyAsync(
                    u => u.UserId == dto.InspectedBy.Value))
            {
                return BadRequest(new { message = "Inspector not found." });
            }

            var inspectionDate = dto.InspectionDate ?? DateTime.UtcNow;

            if (inspectionDate.Kind == DateTimeKind.Unspecified)
                inspectionDate = DateTime.SpecifyKind(
                    inspectionDate, DateTimeKind.Utc);
            else
                inspectionDate = inspectionDate.ToUniversalTime();

            var inspection = new QualityInspection
            {
                LotId = dto.LotId,
                ReceiptId = dto.ReceiptId,
                ProductionOrderId = dto.ProductionOrderId,
                InspectionType = inspectionType,
                InspectionDate = inspectionDate,
                InspectedBy = dto.InspectedBy,
                OverallResult = "PENDING",
                Remarks = dto.Remarks
            };

            _context.QualityInspections.Add(inspection);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = inspection.InspectionId },
                new
                {
                    message = "Quality inspection created successfully.",
                    inspection.InspectionId,
                    inspection.LotId,
                    inspection.InspectionType,
                    inspection.InspectionDate,
                    inspection.OverallResult
                });
        }

        // POST: api/quality-inspections/1/lines
        [HttpPost("{id:long}/lines")]
        public async Task<IActionResult> AddLine(
            long id,
            [FromBody] AddQualityInspectionLineDto dto)
        {
            var inspection = await _context.QualityInspections
                .Include(i => i.QualityInspectionLines)
                .FirstOrDefaultAsync(i => i.InspectionId == id);

            if (inspection == null)
                return NotFound(new { message = "Quality inspection not found." });

            if (inspection.OverallResult != "PENDING")
                return BadRequest(new
                {
                    message = "Cannot add lines to a finalized inspection."
                });

            var parameter = await _context.QualityParameters
                .FirstOrDefaultAsync(p => p.ParameterId == dto.ParameterId);

            if (parameter == null)
                return NotFound(new { message = "Quality parameter not found." });

            if (dto.MinimumValue.HasValue &&
                dto.MaximumValue.HasValue &&
                dto.MinimumValue.Value > dto.MaximumValue.Value)
            {
                return BadRequest(new
                {
                    message = "Minimum value cannot be greater than maximum value."
                });
            }

            string result;

            if (!dto.MeasuredValue.HasValue)
            {
                result = "NOT_APPLICABLE";
            }
            else
            {
                var belowMinimum =
                    dto.MinimumValue.HasValue &&
                    dto.MeasuredValue.Value < dto.MinimumValue.Value;

                var aboveMaximum =
                    dto.MaximumValue.HasValue &&
                    dto.MeasuredValue.Value > dto.MaximumValue.Value;

                result = belowMinimum || aboveMaximum ? "FAIL" : "PASS";
            }

            var line = new QualityInspectionLine
            {
                InspectionId = id,
                ParameterId = dto.ParameterId,
                MeasuredValue = dto.MeasuredValue,
                MinimumValue = dto.MinimumValue,
                MaximumValue = dto.MaximumValue,
                Result = result,
                Remarks = dto.Remarks
            };

            _context.QualityInspectionLines.Add(line);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Inspection line added successfully.",
                line.InspectionLineId,
                line.InspectionId,
                line.ParameterId,
                ParameterName = parameter.ParameterName,
                line.MeasuredValue,
                line.MinimumValue,
                line.MaximumValue,
                line.Result,
                line.Remarks
            });
        }

        // POST: api/quality-inspections/1/finalize
        [HttpPost("{id:long}/finalize")]
        public async Task<IActionResult> Finalize(
            long id,
            [FromBody] FinalizeQualityInspectionDto dto)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var inspection = await _context.QualityInspections
                .Include(i => i.QualityInspectionLines)
                .FirstOrDefaultAsync(i => i.InspectionId == id);

            if (inspection == null)
                return NotFound(new { message = "Quality inspection not found." });

            if (inspection.OverallResult != "PENDING")
                return BadRequest(new
                {
                    message = "This inspection has already been finalized."
                });

            if (!inspection.QualityInspectionLines.Any())
                return BadRequest(new
                {
                    message = "Add at least one inspection line before finalizing."
                });

            var lot = await _context.InventoryLots
                .FirstOrDefaultAsync(l => l.LotId == inspection.LotId);

            if (lot == null)
                return NotFound(new { message = "Inventory lot not found." });

            var newStatus = dto.LotQualityStatus?.Trim().ToUpper();

            if (!new[] { "APPROVED", "REJECTED", "QUARANTINED" }
                .Contains(newStatus))
            {
                return BadRequest(new
                {
                    message = "Lot quality status must be APPROVED, REJECTED, or QUARANTINED."
                });
            }

            if (dto.InspectedBy.HasValue &&
                !await _context.AppUsers.AnyAsync(
                    u => u.UserId == dto.InspectedBy.Value))
            {
                return BadRequest(new { message = "Inspector not found." });
            }

            var hasFailedLine = inspection.QualityInspectionLines
                .Any(l => l.Result == "FAIL");

            var overallResult = hasFailedLine ? "FAILED" : "PASSED";

            if (newStatus == "APPROVED" && overallResult == "FAILED")
            {
                return BadRequest(new
                {
                    message = "An inspection with failed results cannot approve the lot."
                });
            }

            if (newStatus == "REJECTED" && overallResult == "PASSED")
            {
                return BadRequest(new
                {
                    message = "An inspection with passing results cannot reject the lot."
                });
            }

            var previousStatus = lot.QualityStatus;

            inspection.OverallResult = overallResult;

            if (dto.InspectedBy.HasValue)
                inspection.InspectedBy = dto.InspectedBy;

            if (!string.IsNullOrWhiteSpace(dto.Remarks))
                inspection.Remarks = dto.Remarks;

            lot.QualityStatus = newStatus;

            var history = new QualityStatusHistory
            {
                LotId = lot.LotId,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = dto.InspectedBy ?? inspection.InspectedBy,
                Reason = dto.Reason
            };

            _context.QualityStatusHistories.Add(history);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Quality inspection finalized successfully.",
                inspection.InspectionId,
                inspection.OverallResult,
                inspection.InspectedBy,
                lot.LotId,
                PreviousQualityStatus = previousStatus,
                NewQualityStatus = lot.QualityStatus
            });
        }
    }
}
