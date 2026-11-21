
using System.Data;
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
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var order = await _context.ProductionOrders
                    .Include(p => p.ProductionMaterialRequirements)
                    .FirstOrDefaultAsync(p => p.ProductionOrderId == id);

                if (order == null)
                {
                    await transaction.RollbackAsync();

                    return NotFound(new
                    {
                        message = "Production order not found."
                    });
                }

                if (order.ProductionStatus != "DRAFT" &&
                    order.ProductionStatus != "PLANNED")
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "Only DRAFT or PLANNED production orders can be released.",
                        currentStatus = order.ProductionStatus
                    });
                }

                if (!order.ProductionMaterialRequirements.Any())
                {
                    await transaction.RollbackAsync();

                    return BadRequest(new
                    {
                        message = "Production order has no material requirements."
                    });
                }

                foreach (var requirement in order.ProductionMaterialRequirements)
                {
                    if (requirement.RequiredQuantity <= 0)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = "Material requirement quantity must be greater than zero.",
                            itemId = requirement.ItemId,
                            requiredQuantity = requirement.RequiredQuantity
                        });
                    }

                    if (requirement.IssuedQuantity < 0 ||
                        requirement.ReservedQuantity < 0)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = "Issued and reserved quantities cannot be negative.",
                            itemId = requirement.ItemId,
                            issuedQuantity = requirement.IssuedQuantity,
                            reservedQuantity = requirement.ReservedQuantity
                        });
                    }

                    var outstandingQuantity =
                        requirement.RequiredQuantity -
                        requirement.IssuedQuantity;

                    if (outstandingQuantity < 0)
                    {
                        await transaction.RollbackAsync();

                        return BadRequest(new
                        {
                            message = "Issued quantity cannot exceed the required quantity.",
                            itemId = requirement.ItemId,
                            requiredQuantity = requirement.RequiredQuantity,
                            issuedQuantity = requirement.IssuedQuantity
                        });
                    }

                    if (requirement.ReservedQuantity < outstandingQuantity)
                    {
                        await transaction.RollbackAsync();

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
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
