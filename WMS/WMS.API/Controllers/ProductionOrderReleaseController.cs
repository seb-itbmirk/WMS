
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;

namespace WMS.API.Controllers
{
    [Route("api/production-orders")]
    [ApiController]
    public class ProductionOrderReleaseController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public ProductionOrderReleaseController(WmsDbContext context)
        {
            _context = context;
        }

        // POST: api/production-orders/1/release
        [HttpPost("{id:long}/release")]
        public async Task<IActionResult> Release(long id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.ProductionOrders
                    .Include(p => p.ProductionMaterialRequirements)
                    .FirstOrDefaultAsync(p => p.ProductionOrderId == id);

                if (order == null)
                    return NotFound(new
                    {
                        message = "Production order not found."
                    });

                if (order.ProductionStatus != "DRAFT" &&
                    order.ProductionStatus != "PLANNED")
                    return BadRequest(new
                    {
                        message = "Only DRAFT or PLANNED production orders can be released."
                    });

                if (!order.ProductionMaterialRequirements.Any())
                    return BadRequest(new
                    {
                        message = "Production order has no material requirements."
                    });

                foreach (var requirement in order.ProductionMaterialRequirements)
                {
                    var outstandingQuantity =
                        requirement.RequiredQuantity -
                        requirement.IssuedQuantity;

                    if (requirement.ReservedQuantity < outstandingQuantity)
                        return BadRequest(new
                        {
                            message = "All outstanding material requirements must be reserved before releasing production.",
                            itemId = requirement.ItemId,
                            requiredQuantity = requirement.RequiredQuantity,
                            issuedQuantity = requirement.IssuedQuantity,
                            reservedQuantity = requirement.ReservedQuantity,
                            outstandingQuantity
                        });
                }

                order.ProductionStatus = "RELEASED";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Production order released successfully.",
                    order.ProductionOrderId,
                    order.ProductionNumber,
                    order.ProductionStatus
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
