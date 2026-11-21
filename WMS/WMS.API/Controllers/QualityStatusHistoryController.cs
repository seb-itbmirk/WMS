
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;

namespace WMS.API.Controllers
{
    [Route("api/inventory-lots/{lotId:long}/quality-history")]
    [ApiController]
    public class QualityStatusHistoryController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public QualityStatusHistoryController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/inventory-lots/4/quality-history
        [HttpGet]
        public async Task<IActionResult> GetByLotId(long lotId)
        {
            if (lotId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid inventory lot ID is required."
                });
            }

            var lotExists = await _context.InventoryLots
                .AsNoTracking()
                .AnyAsync(l => l.LotId == lotId);

            if (!lotExists)
            {
                return NotFound(new
                {
                    message = "Inventory lot not found."
                });
            }

            var history = await _context.QualityStatusHistories
                .AsNoTracking()
                .Where(h => h.LotId == lotId)
                .OrderByDescending(h => h.ChangedAt)
                .ThenByDescending(h => h.HistoryId)
                .Select(h => new
                {
                    h.HistoryId,
                    h.LotId,
                    h.PreviousStatus,
                    h.NewStatus,
                    h.ChangedAt,
                    h.ChangedBy,
                    h.Reason
                })
                .ToListAsync();

            return Ok(new
            {
                lotId,
                totalRecords = history.Count,
                history
            });
        }
    }
}
