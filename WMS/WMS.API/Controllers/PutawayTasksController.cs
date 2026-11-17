
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.Models;
using WMS.API.DTOs.PutawayTasks;

namespace WMS.API.Controllers
{
    [Route("api/putaway-tasks")]
    [ApiController]
    public class PutawayTasksController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public PutawayTasksController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/putaway-tasks
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _context.PutawayTasks
                .Include(p => p.ReceiptLine)
                    .ThenInclude(r => r.Item)
                .Include(p => p.Lot)
                .Include(p => p.FromBin)
                .Include(p => p.ToBin)
                .OrderByDescending(p => p.PutawayId)
                .Select(p => new
                {
                    p.PutawayId,
                    p.ReceiptLineId,
                    p.LotId,
                    BatchCode = p.Lot.BatchCode,
                    ItemCode = p.ReceiptLine.Item.ItemCode,
                    ItemName = p.ReceiptLine.Item.ItemName,
                    p.FromBinId,
                    FromBinCode = p.FromBin != null
                        ? p.FromBin.BinCode : null,
                    p.ToBinId,
                    ToBinCode = p.ToBin.BinCode,
                    p.Quantity,
                    p.Status,
                    p.AssignedTo,
                    p.CompletedAt
                })
                .ToListAsync();

            return Ok(tasks);
        }

        // GET: api/putaway-tasks/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var task = await _context.PutawayTasks
                .Include(p => p.ReceiptLine)
                    .ThenInclude(r => r.Item)
                .Include(p => p.Lot)
                .Include(p => p.FromBin)
                .Include(p => p.ToBin)
                .FirstOrDefaultAsync(p => p.PutawayId == id);

            if (task == null)
                return NotFound("Putaway task not found.");

            return Ok(new
            {
                task.PutawayId,
                task.ReceiptLineId,
                task.LotId,
                task.Lot.BatchCode,
                ItemCode = task.ReceiptLine.Item.ItemCode,
                ItemName = task.ReceiptLine.Item.ItemName,
                task.FromBinId,
                FromBinCode = task.FromBin?.BinCode,
                task.ToBinId,
                ToBinCode = task.ToBin.BinCode,
                task.Quantity,
                task.Status,
                task.AssignedTo,
                task.CompletedAt
            });
        }

        // POST: api/putaway-tasks
        [HttpPost]
        public async Task<IActionResult> Create(
            CreatePutawayTaskDto dto)
        {
            if (dto.Quantity <= 0)
                return BadRequest(
                    "Quantity must be greater than zero.");

            var receiptLine = await _context.GoodsReceiptLines
                .FirstOrDefaultAsync(r =>
                    r.ReceiptLineId == dto.ReceiptLineId);

            if (receiptLine == null)
                return BadRequest(
                    "Goods receipt line does not exist.");

            if (receiptLine.LotId != dto.LotId)
                return BadRequest(
                    "The selected lot does not match the receipt line.");

            var toBin = await _context.StorageBins
                .FirstOrDefaultAsync(b =>
                    b.BinId == dto.ToBinId && b.IsActive);

            if (toBin == null)
                return BadRequest(
                    "Destination bin does not exist or is inactive.");

            if (dto.FromBinId.HasValue)
            {
                if (dto.FromBinId.Value == dto.ToBinId)
                    return BadRequest(
                        "Source and destination bins cannot be the same.");

                var fromBin = await _context.StorageBins
                    .FirstOrDefaultAsync(b =>
                        b.BinId == dto.FromBinId.Value && b.IsActive);

                if (fromBin == null)
                    return BadRequest(
                        "Source bin does not exist or is inactive.");
            }

            decimal alreadyAssigned = await _context.PutawayTasks
                .Where(p =>
                    p.ReceiptLineId == dto.ReceiptLineId &&
                    p.Status != "CANCELLED")
                .SumAsync(p => (decimal?)p.Quantity) ?? 0;

            decimal remainingQuantity =
                receiptLine.ReceivedQuantity - alreadyAssigned;

            if (dto.Quantity > remainingQuantity)
                return BadRequest(
                    $"Putaway quantity exceeds remaining quantity. Remaining: {remainingQuantity}.");

            if (dto.AssignedTo.HasValue)
            {
                bool userExists = await _context.AppUsers
                    .AnyAsync(u =>
                        u.UserId == dto.AssignedTo.Value);

                if (!userExists)
                    return BadRequest(
                        "Assigned user does not exist.");
            }

            var putawayTask = new PutawayTask
            {
                ReceiptLineId = dto.ReceiptLineId,
                LotId = dto.LotId,
                FromBinId = dto.FromBinId,
                ToBinId = dto.ToBinId,
                Quantity = dto.Quantity,
                Status = "PENDING",
                AssignedTo = dto.AssignedTo
            };

            _context.PutawayTasks.Add(putawayTask);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = putawayTask.PutawayId },
                new
                {
                    message = "Putaway task created successfully.",
                    putawayId = putawayTask.PutawayId,
                    status = putawayTask.Status
                });
        }
    }
}
