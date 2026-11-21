
using System.Data;
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
                .AsNoTracking()
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
                .AsNoTracking()
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
            {
                return NotFound(new
                {
                    message = "Quality inspection not found."
                });
            }

            return Ok(inspection);
        }

        // POST: api/quality-inspections
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateQualityInspectionDto dto)
        {
            if (dto == null || dto.LotId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid inventory lot is required."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.InspectionType))
            {
                return BadRequest(new
                {
                    message = "Inspection type is required."
                });
            }

            var inspectionType = dto.InspectionType
                .Trim()
                .ToUpperInvariant();

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
            {
                return NotFound(new
                {
                    message = "Inventory lot not found."
                });
            }

            if (dto.ReceiptId.HasValue)
            {
                var receiptLineExists =
                    await _context.GoodsReceiptLines.AnyAsync(r =>
                        r.ReceiptId == dto.ReceiptId.Value &&
                        r.LotId == dto.LotId);

                if (!receiptLineExists)
                {
                    return BadRequest(new
                    {
                        message = "The selected goods receipt does not contain this inventory lot."
                    });
                }
            }

            if (dto.ProductionOrderId.HasValue)
            {
                var productionOrderExists =
                    await _context.ProductionOrders.AnyAsync(p =>
                        p.ProductionOrderId ==
                        dto.ProductionOrderId.Value);

                if (!productionOrderExists)
                {
                    return BadRequest(new
                    {
                        message = "Production order not found."
                    });
                }

                var isProductionOutput =
                    await _context.ProductionOutputs.AnyAsync(o =>
                        o.ProductionOrderId ==
                            dto.ProductionOrderId.Value &&
                        o.FinishedLotId == dto.LotId);

                if (!isProductionOutput)
                {
                    return BadRequest(new
                    {
                        message = "The selected lot is not a finished output of this production order."
                    });
                }
            }

            if (inspectionType == "RECEIVING" &&
                !dto.ReceiptId.HasValue)
            {
                return BadRequest(new
                {
                    message = "A goods receipt is required for a RECEIVING inspection."
                });
            }

            if ((inspectionType == "IN_PROCESS" ||
                 inspectionType == "FINAL") &&
                !dto.ProductionOrderId.HasValue)
            {
                return BadRequest(new
                {
                    message = "A production order is required for this inspection type."
                });
            }

            if (dto.InspectedBy.HasValue)
            {
                var inspectorExists = await _context.AppUsers
                    .AnyAsync(u => u.UserId == dto.InspectedBy.Value);

                if (!inspectorExists)
                {
                    return BadRequest(new
                    {
                        message = "Inspector not found."
                    });
                }
            }

            var inspectionDate =
                dto.InspectionDate ?? DateTime.UtcNow;

            if (inspectionDate.Kind == DateTimeKind.Unspecified)
            {
                inspectionDate = DateTime.SpecifyKind(
                    inspectionDate,
                    DateTimeKind.Utc);
            }
            else
            {
                inspectionDate = inspectionDate.ToUniversalTime();
            }

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
            if (dto == null || dto.ParameterId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid quality parameter is required."
                });
            }

            if (dto.MinimumValue.HasValue &&
                dto.MaximumValue.HasValue &&
                dto.MinimumValue.Value > dto.MaximumValue.Value)
            {
                return BadRequest(new
                {
                    message = "Minimum value cannot be greater than maximum value."
                });
            }

            var inspection = await _context.QualityInspections
                .Include(i => i.QualityInspectionLines)
                .FirstOrDefaultAsync(i => i.InspectionId == id);

            if (inspection == null)
            {
                return NotFound(new
                {
                    message = "Quality inspection not found."
                });
            }

            if (inspection.OverallResult != "PENDING")
            {
                return BadRequest(new
                {
                    message = "Cannot add lines to a finalized inspection."
                });
            }

            var parameter = await _context.QualityParameters
                .FirstOrDefaultAsync(p =>
                    p.ParameterId == dto.ParameterId);

            if (parameter == null)
            {
                return NotFound(new
                {
                    message = "Quality parameter not found."
                });
            }

            var duplicateParameter =
                inspection.QualityInspectionLines.Any(l =>
                    l.ParameterId == dto.ParameterId);

            if (duplicateParameter)
            {
                return Conflict(new
                {
                    message = "This quality parameter has already been added to the inspection."
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

                result = belowMinimum || aboveMaximum
                    ? "FAIL"
                    : "PASS";
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
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.LotQualityStatus))
            {
                return BadRequest(new
                {
                    message = "A lot quality status is required."
                });
            }

            var newStatus = dto.LotQualityStatus
                .Trim()
                .ToUpperInvariant();

            if (!new[] { "APPROVED", "REJECTED", "QUARANTINED" }
                .Contains(newStatus))
            {
                return BadRequest(new
                {
                    message = "Lot quality status must be APPROVED, REJECTED, or QUARANTINED."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var inspection = await _context.QualityInspections
                    .Include(i => i.QualityInspectionLines)
                    .FirstOrDefaultAsync(i => i.InspectionId == id);

                if (inspection == null)
                {
                    return NotFound(new
                    {
                        message = "Quality inspection not found."
                    });
                }

                if (inspection.OverallResult != "PENDING")
                {
                    return BadRequest(new
                    {
                        message = "This inspection has already been finalized."
                    });
                }

                if (!inspection.QualityInspectionLines.Any())
                {
                    return BadRequest(new
                    {
                        message = "Add at least one inspection line before finalizing."
                    });
                }

                if (!inspection.QualityInspectionLines.Any(
                    l => l.Result == "PASS" || l.Result == "FAIL"))
                {
                    return BadRequest(new
                    {
                        message = "At least one inspection line must have a measured result."
                    });
                }

                var lot = await _context.InventoryLots
                    .FirstOrDefaultAsync(l =>
                        l.LotId == inspection.LotId);

                if (lot == null)
                {
                    return NotFound(new
                    {
                        message = "Inventory lot not found."
                    });
                }

                if (dto.InspectedBy.HasValue)
                {
                    var inspectorExists = await _context.AppUsers
                        .AnyAsync(u =>
                            u.UserId == dto.InspectedBy.Value);

                    if (!inspectorExists)
                    {
                        return BadRequest(new
                        {
                            message = "Inspector not found."
                        });
                    }
                }

                var hasFailedLine =
                    inspection.QualityInspectionLines.Any(
                        l => l.Result == "FAIL");

                var overallResult = hasFailedLine
                    ? "FAILED"
                    : "PASSED";

                if (newStatus == "APPROVED" &&
                    overallResult == "FAILED")
                {
                    return BadRequest(new
                    {
                        message = "An inspection with failed results cannot approve the lot."
                    });
                }

                if (newStatus == "REJECTED" &&
                    overallResult == "PASSED")
                {
                    return BadRequest(new
                    {
                        message = "An inspection with passing results cannot reject the lot."
                    });
                }

                var previousStatus = lot.QualityStatus;

                inspection.OverallResult = overallResult;

                if (dto.InspectedBy.HasValue)
                {
                    inspection.InspectedBy = dto.InspectedBy;
                }

                if (!string.IsNullOrWhiteSpace(dto.Remarks))
                {
                    inspection.Remarks = dto.Remarks;
                }

                lot.QualityStatus = newStatus;

                var history = new QualityStatusHistory
                {
                    LotId = lot.LotId,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = dto.InspectedBy ??
                                inspection.InspectedBy,
                    Reason = dto.Reason
                };

                _context.QualityStatusHistories.Add(history);

                var inventoryReleased = false;
                long? inventoryTransactionId = null;

                // Release quarantine stock only when the lot is approved.
                if (newStatus == "APPROVED")
                {
                    var quarantineBalances =
                        await _context.InventoryBalances
                            .Where(b =>
                                b.LotId == lot.LotId &&
                                b.StockStatus == "QUARANTINE" &&
                                b.QuantityOnHand > 0)
                            .ToListAsync();

                    if (quarantineBalances.Any())
                    {
                        var item = await _context.Items
                            .FirstOrDefaultAsync(i =>
                                i.ItemId == lot.ItemId);

                        if (item == null)
                        {
                            return BadRequest(new
                            {
                                message = "Item linked to this lot was not found."
                            });
                        }

                        foreach (var balance in quarantineBalances)
                        {
                            if (balance.QuantityReserved >
                                balance.QuantityOnHand ||
                                balance.QuantityReserved < 0)
                            {
                                return BadRequest(new
                                {
                                    message = $"Invalid reserved quantity for inventory balance {balance.BalanceId}."
                                });
                            }
                        }

                        var now = DateTime.UtcNow;

                        var inventoryTransaction =
                            new InventoryTransaction
                            {
                                TransactionNumber =
                                    $"QIR-{Guid.NewGuid():N}",
                                TransactionType = "TRANSFER",
                                TransactionDate = now,
                                ReferenceNumber =
                                    $"QUALITY-INSPECTION-{inspection.InspectionId}",
                                Remarks =
                                    "Quality approval: QUARANTINE to AVAILABLE",
                                Status = "POSTED",
                                PostedAt = now,
                                PostedBy = dto.InspectedBy ??
                                           inspection.InspectedBy
                            };

                        _context.InventoryTransactions.Add(
                            inventoryTransaction);

                        await _context.SaveChangesAsync();

                        foreach (var quarantineBalance
                                 in quarantineBalances)
                        {
                            var quantityToRelease =
                                quarantineBalance.QuantityOnHand;

                            var reservedQuantity =
                                quarantineBalance.QuantityReserved;

                            var availableBalance =
                                await _context.InventoryBalances
                                    .FirstOrDefaultAsync(b =>
                                        b.LotId == lot.LotId &&
                                        b.BinId == quarantineBalance.BinId &&
                                        b.StockStatus == "AVAILABLE");

                            if (availableBalance == null)
                            {
                                availableBalance =
                                    new InventoryBalance
                                    {
                                        LotId = lot.LotId,
                                        BinId = quarantineBalance.BinId,
                                        StockStatus = "AVAILABLE",
                                        QuantityOnHand = 0,
                                        QuantityReserved = 0,
                                        UpdatedAt = now
                                    };

                                _context.InventoryBalances.Add(
                                    availableBalance);
                            }

                            availableBalance.QuantityOnHand +=
                                quantityToRelease;

                            availableBalance.QuantityReserved +=
                                reservedQuantity;

                            availableBalance.UpdatedAt = now;

                            quarantineBalance.QuantityOnHand = 0;
                            quarantineBalance.QuantityReserved = 0;
                            quarantineBalance.UpdatedAt = now;

                            var transactionLine =
                                new InventoryTransactionLine
                                {
                                    TransactionId =
                                        inventoryTransaction.TransactionId,
                                    LotId = lot.LotId,
                                    FromBinId =
                                        quarantineBalance.BinId,
                                    ToBinId =
                                        quarantineBalance.BinId,
                                    Quantity = quantityToRelease,
                                    UomId = item.BaseUomId,
                                    LineRemarks =
                                        "QUALITY RELEASE: QUARANTINE to AVAILABLE"
                                };

                            _context.InventoryTransactionLines.Add(
                                transactionLine);
                        }

                        inventoryReleased = true;
                        inventoryTransactionId =
                            inventoryTransaction.TransactionId;
                    }
                }

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
                    NewQualityStatus = lot.QualityStatus,
                    InventoryReleased = inventoryReleased,
                    InventoryTransactionId = inventoryTransactionId
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
