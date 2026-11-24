
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using WMS.API.Data;
using WMS.API.DTOs.Replenishment;
using WMS.API.Models;

namespace WMS.API.Controllers;

[Authorize]
[ApiController]
[Route("api/replenishment-tasks")]
public class ReplenishmentTasksController : ControllerBase
{
    private readonly WmsDbContext _context;

    public ReplenishmentTasksController(WmsDbContext context)
    {
        _context = context;
    }

    private bool TryGetCurrentUserId(out long userId)
    {
        return long.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }

    private async Task<bool> HasWarehouseAccessAsync(
        long userId,
        long warehouseId)
    {
        return await _context.Database.SqlQuery<bool>($"""
            SELECT EXISTS (
                SELECT 1
                FROM user_warehouse_access
                WHERE user_id = {userId}
                  AND warehouse_id = {warehouseId}
            ) AS "Value"
            """).SingleAsync();
    }

    private async Task<List<long>> GetAccessibleWarehouseIdsAsync(
        long userId)
    {
        return await _context.Database.SqlQuery<long>($"""
            SELECT warehouse_id AS "Value"
            FROM user_warehouse_access
            WHERE user_id = {userId}
            """).ToListAsync();
    }

    // =========================================================
    // GET ALL REPLENISHMENT TASKS
    // GET: api/replenishment-tasks
    // =========================================================

    [HttpGet]
    [Authorize(Policy = "Permission:inventory.replenish.execute")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] long? warehouseId,
        [FromQuery] string? status,
        [FromQuery] long? assignedTo,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new
            {
                message = "Page must be at least 1 and pageSize must be between 1 and 100."
            });

        if (warehouseId.HasValue && warehouseId.Value <= 0)
            return BadRequest(new { message = "Invalid warehouse ID." });

        if (assignedTo.HasValue && assignedTo.Value <= 0)
            return BadRequest(new { message = "Invalid assigned employee ID." });

        if (createdFrom.HasValue &&
            createdTo.HasValue &&
            createdFrom.Value > createdTo.Value)
        {
            return BadRequest(new
            {
                message = "Created-from date cannot be later than created-to date."
            });
        }

        var warehouseIds =
            await GetAccessibleWarehouseIdsAsync(userId);

        var query = _context.ReplenishmentTasks
            .AsNoTracking()
            .Where(t => warehouseIds.Contains(t.WarehouseId));

        if (warehouseId.HasValue)
        {
            if (!warehouseIds.Contains(warehouseId.Value))
                return NotFound(new
                {
                    message = "Warehouse not found."
                });

            query = query.Where(t =>
                t.WarehouseId == warehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();

            var allowedStatuses = new[]
            {
                "DRAFT",
                "IN_PROGRESS",
                "COMPLETED",
                "CANCELLED"
            };

            if (!allowedStatuses.Contains(normalizedStatus))
                return BadRequest(new
                {
                    message = "Invalid task status.",
                    allowedStatuses
                });

            query = query.Where(t =>
                t.Status == normalizedStatus);
        }

        if (assignedTo.HasValue)
        {
            query = query.Where(t =>
                t.AssignedTo == assignedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();

            query = query.Where(t =>
                t.TaskNumber.Contains(searchTerm) ||
                (t.AssignedToNavigation != null &&
                 t.AssignedToNavigation.FullName.Contains(searchTerm)));
        }

        if (createdFrom.HasValue)
        {
            query = query.Where(t =>
                t.CreatedAt >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            query = query.Where(t =>
                t.CreatedAt <= createdTo.Value);
        }

        var totalRecords = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize);

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.ReplenishmentTaskId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.ReplenishmentTaskId,
                t.TaskNumber,
                t.WarehouseId,
                WarehouseName = t.Warehouse.WarehouseName,
                t.Status,
                t.CreatedBy,
                t.AssignedTo,

                AssignedEmployee = t.AssignedToNavigation != null
                    ? t.AssignedToNavigation.FullName
                    : null,

                t.CreatedAt,
                t.StartedAt,
                t.CompletedAt,

                TotalLines = t.ReplenishmentTaskLines.Count(),

                TotalRequestedQuantity =
                    t.ReplenishmentTaskLines
                        .Sum(l => (decimal?)l.RequestedQuantity) ?? 0,

                TotalMovedQuantity =
                    t.ReplenishmentTaskLines
                        .Sum(l => (decimal?)l.MovedQuantity) ?? 0
            })
            .ToListAsync();

        return Ok(new
        {
            pagination = new
            {
                page,
                pageSize,
                totalRecords,
                totalPages,
                hasPreviousPage = page > 1,
                hasNextPage = page < totalPages
            },
            filters = new
            {
                search,
                warehouseId,
                status,
                assignedTo,
                createdFrom,
                createdTo
            },
            data = tasks
        });
    }

    // =========================================================
    // GET REPLENISHMENT TASK DETAILS WITH LINES
    // GET: api/replenishment-tasks/{id}
    // =========================================================

    [HttpGet("{id:long}")]
    [Authorize(Policy = "Permission:inventory.replenish.execute")]
    public async Task<IActionResult> GetById(long id)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var warehouseIds =
            await GetAccessibleWarehouseIdsAsync(userId);

        var task = await _context.ReplenishmentTasks
            .AsNoTracking()
            .Where(t =>
                t.ReplenishmentTaskId == id &&
                warehouseIds.Contains(t.WarehouseId))
            .Select(t => new
            {
                t.ReplenishmentTaskId,
                t.TaskNumber,
                t.WarehouseId,
                WarehouseName = t.Warehouse.WarehouseName,
                t.Status,
                t.CreatedBy,
                t.AssignedTo,

                AssignedEmployee = t.AssignedToNavigation != null
                    ? t.AssignedToNavigation.FullName
                    : null,

                t.CompletedBy,
                t.CreatedAt,
                t.StartedAt,
                t.CompletedAt,
                t.Notes,

                TotalLines = t.ReplenishmentTaskLines.Count(),

                TotalRequestedQuantity =
                    t.ReplenishmentTaskLines
                        .Sum(l => (decimal?)l.RequestedQuantity) ?? 0,

                TotalMovedQuantity =
                    t.ReplenishmentTaskLines
                        .Sum(l => (decimal?)l.MovedQuantity) ?? 0,

                Lines = t.ReplenishmentTaskLines
                    .OrderBy(l => l.ReplenishmentTaskLineId)
                    .Select(l => new
                    {
                        l.ReplenishmentTaskLineId,
                        l.LotId,

                        ItemId = l.Lot.ItemId,
                        l.Lot.BatchCode,
                        l.Lot.SupplierBatchCode,
                        l.Lot.ManufactureDate,
                        l.Lot.ExpiryDate,
                        l.Lot.ReceivedDate,
                        l.Lot.QualityStatus,

                        l.SourceBinId,
                        SourceBinCode = l.SourceBin.BinCode,

                        l.DestinationBinId,
                        DestinationBinCode = l.DestinationBin.BinCode,

                        l.RequestedQuantity,
                        l.MovedQuantity,

                        RemainingQuantity =
                            l.RequestedQuantity - l.MovedQuantity,

                        l.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (task == null)
            return NotFound(new
            {
                message = "Replenishment task not found."
            });

        return Ok(task);
    }

    // =========================================================
    // CREATE REPLENISHMENT TASK
    // POST: api/replenishment-tasks
    // =========================================================

    [HttpPost]
    [Authorize(Policy = "Permission:inventory.replenish")]
    public async Task<IActionResult> Create(
        [FromBody] CreateReplenishmentTaskDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        if (dto.WarehouseId <= 0)
            return BadRequest(new
            {
                message = "Invalid warehouse ID."
            });

        if (!await HasWarehouseAccessAsync(userId, dto.WarehouseId))
            return Forbid();

        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w =>
                w.WarehouseId == dto.WarehouseId &&
                w.IsActive);

        if (warehouse == null)
            return BadRequest(new
            {
                message = "Warehouse does not exist or is inactive."
            });

        var taskNumber = dto.TaskNumber?.Trim();

        if (string.IsNullOrWhiteSpace(taskNumber))
            return BadRequest(new
            {
                message = "Task number is required."
            });

        var duplicateTaskNumber = await _context.ReplenishmentTasks
            .AnyAsync(t => t.TaskNumber == taskNumber);

        if (duplicateTaskNumber)
            return Conflict(new
            {
                message = "Task number already exists."
            });

        if (dto.AssignedTo.HasValue)
        {
            if (dto.AssignedTo.Value <= 0)
                return BadRequest(new
                {
                    message = "Invalid assigned user ID."
                });

            var assigneeHasAccess =
                await HasWarehouseAccessAsync(
                    dto.AssignedTo.Value,
                    dto.WarehouseId);

            if (!assigneeHasAccess)
                return BadRequest(new
                {
                    message = "Assigned user does not have access to this warehouse."
                });

            var assigneeIsActive = await _context.AppUsers
                .AnyAsync(u =>
                    u.UserId == dto.AssignedTo.Value &&
                    u.IsActive);

            if (!assigneeIsActive)
                return BadRequest(new
                {
                    message = "Assigned user does not exist or is inactive."
                });
        }

        var task = new ReplenishmentTask
        {
            TaskNumber = taskNumber,
            WarehouseId = dto.WarehouseId,
            Status = "DRAFT",
            CreatedBy = userId,
            AssignedTo = dto.AssignedTo,
            CreatedAt = DateTime.UtcNow,
            Notes = dto.Notes
        };

        _context.ReplenishmentTasks.Add(task);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = task.ReplenishmentTaskId },
            new
            {
                message = "Replenishment task created successfully.",
                task.ReplenishmentTaskId,
                task.TaskNumber,
                task.WarehouseId,
                task.Status,
                task.CreatedBy,
                task.AssignedTo,
                task.CreatedAt
            });
    }

    // =========================================================
    // ADD REPLENISHMENT TASK LINE
    // POST: api/replenishment-tasks/{id}/lines
    // =========================================================

    [HttpPost("{id:long}/lines")]
    [Authorize(Policy = "Permission:inventory.replenish")]
    public async Task<IActionResult> AddLine(
        long id,
        [FromBody] AddReplenishmentTaskLineDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        if (dto.LotId <= 0 ||
            dto.SourceBinId <= 0 ||
            dto.DestinationBinId <= 0 ||
            dto.RequestedQuantity <= 0)
        {
            return BadRequest(new
            {
                message = "Invalid replenishment line information."
            });
        }

        var warehouseIds =
            await GetAccessibleWarehouseIdsAsync(userId);

        var task = await _context.ReplenishmentTasks
            .FirstOrDefaultAsync(t =>
                t.ReplenishmentTaskId == id &&
                warehouseIds.Contains(t.WarehouseId));

        if (task == null)
            return NotFound(new
            {
                message = "Replenishment task not found."
            });

        if (task.Status != "DRAFT")
            return BadRequest(new
            {
                message = "Lines can only be added to DRAFT tasks."
            });

        if (dto.SourceBinId == dto.DestinationBinId)
            return BadRequest(new
            {
                message = "Source and destination bins cannot be the same."
            });

        var bins = await _context.StorageBins
            .Where(b =>
                (b.BinId == dto.SourceBinId ||
                 b.BinId == dto.DestinationBinId) &&
                b.IsActive)
            .Select(b => new
            {
                b.BinId,
                b.BinCode,
                b.IsActive,
                WarehouseId = b.Zone.WarehouseId
            })
            .ToListAsync();

        var sourceBin = bins.FirstOrDefault(
            b => b.BinId == dto.SourceBinId);

        var destinationBin = bins.FirstOrDefault(
            b => b.BinId == dto.DestinationBinId);

        if (sourceBin == null || destinationBin == null)
            return BadRequest(new
            {
                message = "Source or destination bin does not exist or is inactive."
            });

        if (sourceBin.WarehouseId != task.WarehouseId ||
            destinationBin.WarehouseId != task.WarehouseId)
        {
            return BadRequest(new
            {
                message = "Both bins must belong to the task's warehouse."
            });
        }

        var lotExists = await _context.InventoryLots
            .AnyAsync(l => l.LotId == dto.LotId);

        if (!lotExists)
            return BadRequest(new
            {
                message = "Inventory lot not found."
            });

        var duplicateLine = await _context.ReplenishmentTaskLines
            .AnyAsync(l =>
                l.ReplenishmentTaskId == id &&
                l.LotId == dto.LotId &&
                l.SourceBinId == dto.SourceBinId &&
                l.DestinationBinId == dto.DestinationBinId);

        if (duplicateLine)
            return Conflict(new
            {
                message = "This replenishment line already exists."
            });

        var availableQuantity =
            await _context.InventoryBalances
                .Where(b =>
                    b.LotId == dto.LotId &&
                    b.BinId == dto.SourceBinId &&
                    b.StockStatus == "AVAILABLE")
                .SumAsync(b =>
                    (decimal?)(b.QuantityOnHand - b.QuantityReserved))
                ?? 0;

        if (dto.RequestedQuantity > availableQuantity)
            return BadRequest(new
            {
                message = "Requested quantity exceeds available source quantity.",
                availableQuantity
            });

        var line = new ReplenishmentTaskLine
        {
            ReplenishmentTaskId = id,
            LotId = dto.LotId,
            SourceBinId = dto.SourceBinId,
            DestinationBinId = dto.DestinationBinId,
            RequestedQuantity = dto.RequestedQuantity,
            MovedQuantity = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReplenishmentTaskLines.Add(line);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Replenishment line added successfully.",
            line.ReplenishmentTaskLineId,
            line.ReplenishmentTaskId,
            line.LotId,
            line.SourceBinId,
            line.DestinationBinId,
            line.RequestedQuantity,
            line.MovedQuantity
        });
    }

    // =========================================================
    // START REPLENISHMENT TASK
    // POST: api/replenishment-tasks/{id}/start
    // =========================================================

    [HttpPost("{id:long}/start")]
    [Authorize(Policy = "Permission:inventory.replenish.execute")]
    public async Task<IActionResult> StartTask(long id)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var warehouseIds =
            await GetAccessibleWarehouseIdsAsync(userId);

        var task = await _context.ReplenishmentTasks
            .FirstOrDefaultAsync(t =>
                t.ReplenishmentTaskId == id &&
                warehouseIds.Contains(t.WarehouseId));

        if (task == null)
            return NotFound(new
            {
                message = "Replenishment task not found."
            });

        if (task.AssignedTo != userId)
            return Forbid();

        if (task.Status != "DRAFT")
            return BadRequest(new
            {
                message = "Only DRAFT tasks can be started."
            });

        var hasLines = await _context.ReplenishmentTaskLines
            .AnyAsync(l => l.ReplenishmentTaskId == id);

        if (!hasLines)
            return BadRequest(new
            {
                message = "Cannot start a task without replenishment lines."
            });

        task.Status = "IN_PROGRESS";
        task.StartedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Replenishment task started successfully.",
            task.ReplenishmentTaskId,
            task.TaskNumber,
            task.Status,
            task.StartedAt
        });
    }

    // =========================================================
    // EXECUTE REPLENISHMENT TASK LINE
    // POST: api/replenishment-tasks/{id}/lines/{lineId}/execute
    // =========================================================

    [HttpPost("{id:long}/lines/{lineId:long}/execute")]
    [Authorize(Policy = "Permission:inventory.replenish.execute")]
    public async Task<IActionResult> ExecuteLine(
        long id,
        long lineId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        var lockedTaskIds = await _context.Database.SqlQuery<long>($"""
            SELECT replenishment_task_id AS "Value"
            FROM replenishment_tasks
            WHERE replenishment_task_id = {id}
              AND warehouse_id IN (
                  SELECT warehouse_id
                  FROM user_warehouse_access
                  WHERE user_id = {userId}
              )
            FOR UPDATE
            """).ToListAsync();

        if (lockedTaskIds.Count == 0)
            return NotFound(new
            {
                message = "Replenishment task not found."
            });

        var task = await _context.ReplenishmentTasks
            .FirstAsync(t => t.ReplenishmentTaskId == id);

        if (task.AssignedTo != userId)
            return Forbid();

        if (task.Status != "IN_PROGRESS")
            return BadRequest(new
            {
                message = "Task must be IN_PROGRESS."
            });

        var lockedLineIds = await _context.Database.SqlQuery<long>($"""
            SELECT replenishment_task_line_id AS "Value"
            FROM replenishment_task_lines
            WHERE replenishment_task_line_id = {lineId}
              AND replenishment_task_id = {id}
            FOR UPDATE
            """).ToListAsync();

        if (lockedLineIds.Count == 0)
            return NotFound(new
            {
                message = "Replenishment task line not found."
            });

        var line = await _context.ReplenishmentTaskLines
            .FirstAsync(l =>
                l.ReplenishmentTaskLineId == lineId &&
                l.ReplenishmentTaskId == id);

        var remainingQuantity =
            line.RequestedQuantity - line.MovedQuantity;

        if (remainingQuantity <= 0)
            return BadRequest(new
            {
                message = "This replenishment line is already completed."
            });

        var binIds = new[]
        {
            line.SourceBinId,
            line.DestinationBinId
        }
        .Distinct()
        .OrderBy(x => x)
        .ToArray();

        var lockedBinIds = await _context.Database
            .SqlQuery<long>($"""
                SELECT bin_id AS "Value"
                FROM storage_bins
                WHERE bin_id IN ({string.Join(",", binIds)})
                ORDER BY bin_id
                FOR UPDATE
                """)
            .ToListAsync();

        if (lockedBinIds.Count != binIds.Length)
            return BadRequest(new
            {
                message = "Source or destination bin no longer exists."
            });

        var bins = await _context.StorageBins
            .Where(b => binIds.Contains(b.BinId))
            .Select(b => new
            {
                b.BinId,
                b.IsActive,
                WarehouseId = b.Zone.WarehouseId
            })
            .ToListAsync();

        if (bins.Any(b =>
            !b.IsActive ||
            b.WarehouseId != task.WarehouseId))
        {
            return BadRequest(new
            {
                message = "Both bins must be active and belong to the task warehouse."
            });
        }

        var lockedBalanceIds = await _context.Database
            .SqlQuery<long>($"""
                SELECT balance_id AS "Value"
                FROM inventory_balances
                WHERE lot_id = {line.LotId}
                  AND bin_id IN ({string.Join(",", binIds)})
                  AND stock_status = 'AVAILABLE'
                ORDER BY bin_id
                FOR UPDATE
                """)
            .ToListAsync();

        var sourceBalance = await _context.InventoryBalances
            .FirstOrDefaultAsync(b =>
                b.LotId == line.LotId &&
                b.BinId == line.SourceBinId &&
                b.StockStatus == "AVAILABLE");

        if (sourceBalance == null)
            return BadRequest(new
            {
                message = "No AVAILABLE inventory exists in the source bin."
            });

        var availableQuantity =
            sourceBalance.QuantityOnHand -
            sourceBalance.QuantityReserved;

        if (availableQuantity < remainingQuantity)
            return BadRequest(new
            {
                message = "Insufficient available quantity in source bin.",
                availableQuantity,
                remainingQuantity
            });

        var destinationBalance =
            await _context.InventoryBalances
                .FirstOrDefaultAsync(b =>
                    b.LotId == line.LotId &&
                    b.BinId == line.DestinationBinId &&
                    b.StockStatus == "AVAILABLE");

        sourceBalance.QuantityOnHand -= remainingQuantity;

        if (destinationBalance == null)
        {
            destinationBalance = new InventoryBalance
            {
                LotId = line.LotId,
                BinId = line.DestinationBinId,
                StockStatus = "AVAILABLE",
                QuantityOnHand = remainingQuantity,
                QuantityReserved = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _context.InventoryBalances.Add(destinationBalance);
        }
        else
        {
            destinationBalance.QuantityOnHand += remainingQuantity;
            destinationBalance.UpdatedAt = DateTime.UtcNow;
        }

        sourceBalance.UpdatedAt = DateTime.UtcNow;
        line.MovedQuantity += remainingQuantity;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            message = "Replenishment inventory moved successfully.",
            replenishmentTaskId = id,
            replenishmentTaskLineId = lineId,
            line.LotId,
            line.SourceBinId,
            line.DestinationBinId,
            movedQuantity = remainingQuantity,
            totalMovedQuantity = line.MovedQuantity,
            remainingTaskQuantity =
                line.RequestedQuantity - line.MovedQuantity,
            status = task.Status
        });
    }

    // =========================================================
    // COMPLETE REPLENISHMENT TASK
    // POST: api/replenishment-tasks/{id}/complete
    // =========================================================

    [HttpPost("{id:long}/complete")]
    [Authorize(Policy = "Permission:inventory.replenish.execute")]
    public async Task<IActionResult> CompleteTask(long id)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted);

        var lockedTaskIds = await _context.Database.SqlQuery<long>($"""
            SELECT replenishment_task_id AS "Value"
            FROM replenishment_tasks
            WHERE replenishment_task_id = {id}
              AND warehouse_id IN (
                  SELECT warehouse_id
                  FROM user_warehouse_access
                  WHERE user_id = {userId}
              )
            FOR UPDATE
            """).ToListAsync();

        if (lockedTaskIds.Count == 0)
            return NotFound(new
            {
                message = "Replenishment task not found."
            });

        var task = await _context.ReplenishmentTasks
            .FirstAsync(t => t.ReplenishmentTaskId == id);

        if (task.AssignedTo != userId)
            return Forbid();

        if (task.Status != "IN_PROGRESS")
            return BadRequest(new
            {
                message = "Only IN_PROGRESS tasks can be completed."
            });

        var lines = await _context.ReplenishmentTaskLines
            .Where(l => l.ReplenishmentTaskId == id)
            .ToListAsync();

        if (lines.Count == 0)
            return BadRequest(new
            {
                message = "Cannot complete a task without lines."
            });

        var incompleteLines = lines
            .Where(l => l.MovedQuantity != l.RequestedQuantity)
            .ToList();

        if (incompleteLines.Count > 0)
            return BadRequest(new
            {
                message = "All replenishment lines must be fully executed before completing the task.",
                incompleteLines = incompleteLines.Select(l => new
                {
                    l.ReplenishmentTaskLineId,
                    l.RequestedQuantity,
                    l.MovedQuantity,
                    remainingQuantity =
                        l.RequestedQuantity - l.MovedQuantity
                })
            });

        task.Status = "COMPLETED";
        task.CompletedBy = userId;
        task.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            message = "Replenishment task completed successfully.",
            task.ReplenishmentTaskId,
            task.TaskNumber,
            task.Status,
            task.CompletedBy,
            task.CompletedAt
        });
    }
}
