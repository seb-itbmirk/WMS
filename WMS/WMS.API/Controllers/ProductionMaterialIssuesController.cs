
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.ProductionMaterialIssues;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/production-material-issues")]
    [ApiController]
    public class ProductionMaterialIssuesController : ControllerBase
    {
        private readonly WmsDbContext _context;
        private readonly ILogger<ProductionMaterialIssuesController> _logger;

        public ProductionMaterialIssuesController(
            WmsDbContext context,
            ILogger<ProductionMaterialIssuesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/production-material-issues
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var issues = await _context.ProductionMaterialIssues
                .OrderByDescending(i => i.IssueDate)
                .Select(i => new
                {
                    i.IssueId,
                    i.IssueNumber,
                    i.ProductionOrderId,
                    i.ProductionOrder.ProductionNumber,
                    i.WarehouseId,
                    i.Warehouse.WarehouseName,
                    i.IssueDate,
                    i.Status,
                    i.IssuedBy,
                    LineCount = i.ProductionMaterialIssueLines.Count
                })
                .ToListAsync();

            return Ok(issues);
        }

        // GET: api/production-material-issues/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var issue = await _context.ProductionMaterialIssues
                .Where(i => i.IssueId == id)
                .Select(i => new
                {
                    i.IssueId,
                    i.IssueNumber,
                    i.ProductionOrderId,
                    i.ProductionOrder.ProductionNumber,
                    i.WarehouseId,
                    i.Warehouse.WarehouseName,
                    i.IssueDate,
                    i.Status,
                    i.IssuedBy,

                    Lines = i.ProductionMaterialIssueLines
                        .Select(l => new
                        {
                            l.IssueLineId,
                            l.LotId,
                            l.Lot.BatchCode,
                            l.Lot.ItemId,
                            ItemCode = l.Lot.Item.ItemCode,
                            ItemName = l.Lot.Item.ItemName,
                            l.BinId,
                            BinCode = l.Bin.BinCode,
                            l.IssuedQuantity,
                            l.UomId,
                            UomCode = l.Uom.UomCode
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(i => i.IssueId == id);

            if (issue == null)
            {
                return NotFound(new
                {
                    message = "Production material issue not found."
                });
            }

            return Ok(issue);
        }

        // POST: api/production-material-issues
        // Creates a DRAFT issue from active reservations.
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductionMaterialIssueDto dto)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var order = await _context.ProductionOrders
                    .Include(p => p.ProductionMaterialRequirements)
                    .FirstOrDefaultAsync(p =>
                        p.ProductionOrderId == dto.ProductionOrderId);

                if (order == null)
                {
                    return NotFound(new
                    {
                        message = "Production order not found."
                    });
                }

                if (order.ProductionStatus != "RELEASED" &&
                    order.ProductionStatus != "IN_PROGRESS")
                {
                    return BadRequest(new
                    {
                        message = "Production order must be RELEASED or IN_PROGRESS before creating a material issue."
                    });
                }

                var existingDraft = await _context.ProductionMaterialIssues
                    .AnyAsync(i =>
                        i.ProductionOrderId == order.ProductionOrderId &&
                        i.Status == "DRAFT");

                if (existingDraft)
                {
                    return Conflict(new
                    {
                        message = "A draft material issue already exists for this production order."
                    });
                }

                var activeReservations = await _context.InventoryReservations
                    .Where(r =>
                        r.ReferenceType == "PRODUCTION_ORDER" &&
                        r.ReferenceId == order.ProductionOrderId &&
                        r.Status == "ACTIVE")
                    .Include(r => r.Lot)
                    .ToListAsync();

                if (!activeReservations.Any())
                {
                    return BadRequest(new
                    {
                        message = "No active material reservations were found."
                    });
                }

                // Check outstanding requirements.
                foreach (var requirement in order.ProductionMaterialRequirements)
                {
                    var reservedForItem = activeReservations
                        .Where(r => r.Lot.ItemId == requirement.ItemId)
                        .Sum(r => r.ReservedQuantity);

                    var outstandingQuantity =
                        requirement.RequiredQuantity -
                        requirement.IssuedQuantity;

                    if (reservedForItem < outstandingQuantity)
                    {
                        return BadRequest(new
                        {
                            message = "Active reservations do not cover the outstanding material requirements.",
                            itemId = requirement.ItemId,
                            requiredQuantity = requirement.RequiredQuantity,
                            issuedQuantity = requirement.IssuedQuantity,
                            outstandingQuantity,
                            activeReservedQuantity = reservedForItem
                        });
                    }
                }

                // Group reservations by lot and bin.
                var groupedReservations = activeReservations
                    .GroupBy(r => new
                    {
                        r.LotId,
                        r.BinId
                    })
                    .Select(g => new
                    {
                        g.Key.LotId,
                        g.Key.BinId,
                        Quantity = g.Sum(r => r.ReservedQuantity)
                    })
                    .ToList();

                var issue = new ProductionMaterialIssue
                {
                    IssueNumber = "PMI-" +
                        Guid.NewGuid().ToString("N").ToUpper(),

                    ProductionOrderId = order.ProductionOrderId,
                    WarehouseId = order.WarehouseId,
                    IssueDate = DateTime.UtcNow,
                    Status = "DRAFT",
                    IssuedBy = dto.IssuedBy
                };

                foreach (var group in groupedReservations)
                {
                    var lot = await _context.InventoryLots
                        .FirstAsync(l => l.LotId == group.LotId);

                    var requirement = order.ProductionMaterialRequirements
                        .FirstOrDefault(r => r.ItemId == lot.ItemId);

                    if (requirement == null)
                    {
                        return BadRequest(new
                        {
                            message = $"No production material requirement exists for lot {group.LotId}."
                        });
                    }

                    issue.ProductionMaterialIssueLines.Add(
                        new ProductionMaterialIssueLine
                        {
                            LotId = group.LotId,
                            BinId = group.BinId,
                            IssuedQuantity = group.Quantity,
                            UomId = requirement.UomId
                        });
                }

                _context.ProductionMaterialIssues.Add(issue);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = issue.IssueId },
                    new
                    {
                        message = "Draft production material issue created successfully.",
                        issue.IssueId,
                        issue.IssueNumber,
                        issue.ProductionOrderId,
                        issue.WarehouseId,
                        issue.Status,
                        lineCount = issue.ProductionMaterialIssueLines.Count
                    });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex,
                    "Error creating production material issue.");

                throw;
            }
        }

        // POST: api/production-material-issues/1/post
        // Posts the issue and deducts reserved inventory.
        [HttpPost("{id:long}/post")]
        public async Task<IActionResult> Post(long id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var issue = await _context.ProductionMaterialIssues
                    .Include(i => i.ProductionMaterialIssueLines)
                        .ThenInclude(l => l.Lot)
                    .Include(i => i.ProductionOrder)
                        .ThenInclude(p => p.ProductionMaterialRequirements)
                    .FirstOrDefaultAsync(i => i.IssueId == id);

                if (issue == null)
                {
                    return NotFound(new
                    {
                        message = "Production material issue not found."
                    });
                }

                if (issue.Status != "DRAFT")
                {
                    return BadRequest(new
                    {
                        message = "Only DRAFT material issues can be posted."
                    });
                }

                var order = issue.ProductionOrder;

                if (order.ProductionStatus != "RELEASED" &&
                    order.ProductionStatus != "IN_PROGRESS")
                {
                    return BadRequest(new
                    {
                        message = "Production order must be RELEASED or IN_PROGRESS."
                    });
                }

                if (!issue.ProductionMaterialIssueLines.Any())
                {
                    return BadRequest(new
                    {
                        message = "Material issue has no lines."
                    });
                }

                var activeReservations = await _context.InventoryReservations
                    .Where(r =>
                        r.ReferenceType == "PRODUCTION_ORDER" &&
                        r.ReferenceId == order.ProductionOrderId &&
                        r.Status == "ACTIVE")
                    .ToListAsync();

                if (!activeReservations.Any())
                {
                    return BadRequest(new
                    {
                        message = "No active reservations are available for issuance."
                    });
                }

                // Check every issue line against active reservations.
                foreach (var line in issue.ProductionMaterialIssueLines)
                {
                    var reservedQuantity = activeReservations
                        .Where(r =>
                            r.LotId == line.LotId &&
                            r.BinId == line.BinId)
                        .Sum(r => r.ReservedQuantity);

                    if (reservedQuantity < line.IssuedQuantity)
                    {
                        return BadRequest(new
                        {
                            message = "Issue quantity exceeds the active reservation.",
                            lotId = line.LotId,
                            binId = line.BinId,
                            issueQuantity = line.IssuedQuantity,
                            reservedQuantity
                        });
                    }
                }

                // Check requirement quantities before modifying balances.
                foreach (var requirement in order.ProductionMaterialRequirements)
                {
                    var issuedForItem = issue.ProductionMaterialIssueLines
                        .Where(l => l.Lot.ItemId == requirement.ItemId)
                        .Sum(l => l.IssuedQuantity);

                    var outstandingQuantity =
                        requirement.RequiredQuantity -
                        requirement.IssuedQuantity;

                    if (issuedForItem > outstandingQuantity)
                    {
                        return BadRequest(new
                        {
                            message = "Issue quantity exceeds the outstanding material requirement.",
                            itemId = requirement.ItemId,
                            outstandingQuantity,
                            issuedQuantity = issuedForItem
                        });
                    }

                    if (issuedForItem > requirement.ReservedQuantity)
                    {
                        return BadRequest(new
                        {
                            message = "Issued quantity exceeds the reserved requirement quantity.",
                            itemId = requirement.ItemId,
                            reservedQuantity = requirement.ReservedQuantity,
                            issuedQuantity = issuedForItem
                        });
                    }
                }

                var now = DateTime.UtcNow;

                var inventoryTransaction = new InventoryTransaction
                {
                    TransactionNumber = "PROD-ISSUE-" +
                        Guid.NewGuid().ToString("N").ToUpper(),

                    TransactionType = "PRODUCTION_ISSUE",
                    TransactionDate = now,
                    ReferenceNumber = issue.IssueNumber,
                    Remarks = $"Material issue for {order.ProductionNumber}",
                    Status = "POSTED",
                    CreatedBy = issue.IssuedBy,
                    PostedBy = issue.IssuedBy,
                    PostedAt = now
                };

                // Deduct inventory and create transaction lines.
                foreach (var line in issue.ProductionMaterialIssueLines)
                {
                    var balance = await _context.InventoryBalances
                        .FirstOrDefaultAsync(b =>
                            b.LotId == line.LotId &&
                            b.BinId == line.BinId &&
                            b.StockStatus == "AVAILABLE");

                    if (balance == null)
                    {
                        return BadRequest(new
                        {
                            message = $"Available inventory balance not found for lot {line.LotId}, bin {line.BinId}."
                        });
                    }

                    if (balance.QuantityOnHand < line.IssuedQuantity ||
                        balance.QuantityReserved < line.IssuedQuantity)
                    {
                        return BadRequest(new
                        {
                            message = "Inventory balance does not have enough on-hand or reserved quantity.",
                            lotId = line.LotId,
                            binId = line.BinId,
                            onHand = balance.QuantityOnHand,
                            reserved = balance.QuantityReserved,
                            issueQuantity = line.IssuedQuantity
                        });
                    }

                    balance.QuantityOnHand -= line.IssuedQuantity;
                    balance.QuantityReserved -= line.IssuedQuantity;
                    balance.UpdatedAt = now;

                    inventoryTransaction.InventoryTransactionLines.Add(
                        new InventoryTransactionLine
                        {
                            LotId = line.LotId,
                            FromBinId = line.BinId,
                            ToBinId = null,
                            Quantity = line.IssuedQuantity,
                            UomId = line.UomId,
                            LineRemarks = $"Production issue {issue.IssueNumber}"
                        });
                }

                // Fulfill reservations sequentially.
                foreach (var group in issue.ProductionMaterialIssueLines
                    .GroupBy(l => new { l.LotId, l.BinId }))
                {
                    var quantityToFulfill =
                        group.Sum(l => l.IssuedQuantity);

                    var matchingReservations = activeReservations
                        .Where(r =>
                            r.LotId == group.Key.LotId &&
                            r.BinId == group.Key.BinId)
                        .OrderBy(r => r.ReservationId)
                        .ToList();

                    foreach (var reservation in matchingReservations)
                    {
                        if (quantityToFulfill <= 0)
                            break;

                        var fulfilledQuantity = Math.Min(
                            reservation.ReservedQuantity,
                            quantityToFulfill);

                        reservation.ReservedQuantity -= fulfilledQuantity;
                        quantityToFulfill -= fulfilledQuantity;

                        if (reservation.ReservedQuantity == 0)
                        {
                            reservation.Status = "FULFILLED";
                        }
                    }

                    if (quantityToFulfill > 0)
                    {
                        return BadRequest(new
                        {
                            message = "Issued quantity exceeds active reservations.",
                            lotId = group.Key.LotId,
                            binId = group.Key.BinId
                        });
                    }
                }

                // Update production material requirements.
                foreach (var requirement in order.ProductionMaterialRequirements)
                {
                    var issuedForItem = issue.ProductionMaterialIssueLines
                        .Where(l => l.Lot.ItemId == requirement.ItemId)
                        .Sum(l => l.IssuedQuantity);

                    requirement.IssuedQuantity += issuedForItem;
                    requirement.ReservedQuantity -= issuedForItem;
                }

                issue.Status = "POSTED";

                if (order.ProductionStatus == "RELEASED")
                {
                    order.ProductionStatus = "IN_PROGRESS";
                }

                _context.InventoryTransactions.Add(inventoryTransaction);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Production material issue posted successfully.",
                    issueId = issue.IssueId,
                    issueNumber = issue.IssueNumber,
                    productionOrderId = order.ProductionOrderId,
                    productionNumber = order.ProductionNumber,
                    issueStatus = issue.Status,
                    inventoryTransactionId = inventoryTransaction.TransactionId,
                    inventoryTransactionNumber = inventoryTransaction.TransactionNumber,
                    inventoryTransactionType = inventoryTransaction.TransactionType,
                    inventoryTransactionStatus = inventoryTransaction.Status,

                    lines = issue.ProductionMaterialIssueLines.Select(l => new
                    {
                        l.LotId,
                        l.BinId,
                        l.IssuedQuantity,
                        l.UomId
                    })
                });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex,
                    "Database error while posting production material issue {IssueId}. Inner exception: {InnerException}",
                    id,
                    ex.InnerException?.ToString());

                return StatusCode(500, new
                {
                    message = "Database error while posting production material issue.",
                    databaseError = ex.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex,
                    "Error posting production material issue {IssueId}.",
                    id);

                throw;
            }
        }
    }
}
