
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.PurchaseOrders;
using WMS.API.Models;

namespace WMS.API.Controllers
{
    [Route("api/purchase-orders")]
    [ApiController]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public PurchaseOrdersController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/purchase-orders
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var purchaseOrders = await _context.PurchaseOrders
                .OrderByDescending(po => po.OrderDate)
                .ThenByDescending(po => po.PurchaseOrderId)
                .Select(po => new
                {
                    po.PurchaseOrderId,
                    po.PoNumber,
                    po.SupplierId,
                    po.OrderDate,
                    po.ExpectedDeliveryDate,
                    po.Status,
                    po.CreatedAt,
                    LineCount = po.PurchaseOrderLines.Count
                })
                .ToListAsync();

            return Ok(purchaseOrders);
        }

        // GET: api/purchase-orders/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Where(po => po.PurchaseOrderId == id)
                .Select(po => new
                {
                    po.PurchaseOrderId,
                    po.PoNumber,
                    po.SupplierId,
                    po.OrderDate,
                    po.ExpectedDeliveryDate,
                    po.Status,
                    po.CreatedAt,

                    Lines = po.PurchaseOrderLines
                        .OrderBy(line => line.PoLineId)
                        .Select(line => new
                        {
                            line.PoLineId,
                            line.ItemId,
                            ItemCode = line.Item.ItemCode,
                            ItemName = line.Item.ItemName,
                            line.OrderedQuantity,
                            line.UomId,
                            UomCode = line.Uom.UomCode,
                            line.UnitCost
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (purchaseOrder == null)
            {
                return NotFound(new
                {
                    message = "Purchase order not found."
                });
            }

            return Ok(purchaseOrder);
        }

        // POST: api/purchase-orders
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreatePurchaseOrderDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PoNumber))
            {
                return BadRequest(new
                {
                    message = "Purchase order number is required."
                });
            }

            if (dto.OrderDate == default)
            {
                return BadRequest(new
                {
                    message = "Order date is required."
                });
            }

            if (dto.ExpectedDeliveryDate.HasValue &&
                dto.ExpectedDeliveryDate.Value < dto.OrderDate)
            {
                return BadRequest(new
                {
                    message = "Expected delivery date cannot be earlier than order date."
                });
            }

            if (dto.Lines == null || !dto.Lines.Any())
            {
                return BadRequest(new
                {
                    message = "A purchase order must contain at least one item."
                });
            }

            var poNumber = dto.PoNumber.Trim();

            var duplicate = await _context.PurchaseOrders
                .AnyAsync(po => po.PoNumber == poNumber);

            if (duplicate)
            {
                return Conflict(new
                {
                    message = "Purchase order number already exists."
                });
            }

            var supplierExists = await _context.Suppliers
                .AnyAsync(s => s.SupplierId == dto.SupplierId);

            if (!supplierExists)
            {
                return BadRequest(new
                {
                    message = "Supplier does not exist."
                });
            }

            foreach (var line in dto.Lines)
            {
                if (line.OrderedQuantity <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Every ordered quantity must be greater than zero."
                    });
                }

                if (line.UnitCost.HasValue && line.UnitCost.Value < 0)
                {
                    return BadRequest(new
                    {
                        message = "Unit cost cannot be negative."
                    });
                }

                var itemExists = await _context.Items
                    .AnyAsync(i => i.ItemId == line.ItemId && i.IsActive);

                if (!itemExists)
                {
                    return BadRequest(new
                    {
                        message = $"Item ID {line.ItemId} does not exist or is inactive."
                    });
                }

                var uomExists = await _context.UnitsOfMeasures
                    .AnyAsync(u => u.UomId == line.UomId && u.IsActive);

                if (!uomExists)
                {
                    return BadRequest(new
                    {
                        message = $"Unit of measure ID {line.UomId} does not exist or is inactive."
                    });
                }
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var purchaseOrder = new PurchaseOrder
                {
                    PoNumber = poNumber,
                    SupplierId = dto.SupplierId,
                    OrderDate = dto.OrderDate,
                    ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                    Status = "DRAFT",
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var line in dto.Lines)
                {
                    purchaseOrder.PurchaseOrderLines.Add(
                        new PurchaseOrderLine
                        {
                            ItemId = line.ItemId,
                            OrderedQuantity = line.OrderedQuantity,
                            UomId = line.UomId,
                            UnitCost = line.UnitCost
                        });
                }

                _context.PurchaseOrders.Add(purchaseOrder);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = purchaseOrder.PurchaseOrderId },
                    new
                    {
                        message = "Purchase order created successfully.",
                        purchaseOrder.PurchaseOrderId,
                        purchaseOrder.PoNumber,
                        purchaseOrder.SupplierId,
                        purchaseOrder.OrderDate,
                        purchaseOrder.ExpectedDeliveryDate,
                        purchaseOrder.Status,
                        LineCount = purchaseOrder.PurchaseOrderLines.Count
                    });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // PUT: api/purchase-orders/1
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] UpdatePurchaseOrderDto dto)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

            if (purchaseOrder == null)
            {
                return NotFound(new
                {
                    message = "Purchase order not found."
                });
            }

            if (purchaseOrder.Status != "DRAFT")
            {
                return BadRequest(new
                {
                    message = "Only draft purchase orders can be updated."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.PoNumber))
            {
                return BadRequest(new
                {
                    message = "Purchase order number is required."
                });
            }

            if (dto.OrderDate == default)
            {
                return BadRequest(new
                {
                    message = "Order date is required."
                });
            }

            if (dto.ExpectedDeliveryDate.HasValue &&
                dto.ExpectedDeliveryDate.Value < dto.OrderDate)
            {
                return BadRequest(new
                {
                    message = "Expected delivery date cannot be earlier than order date."
                });
            }

            var supplierExists = await _context.Suppliers
                .AnyAsync(s => s.SupplierId == dto.SupplierId);

            if (!supplierExists)
            {
                return BadRequest(new
                {
                    message = "Supplier does not exist."
                });
            }

            var poNumber = dto.PoNumber.Trim();

            var duplicate = await _context.PurchaseOrders
                .AnyAsync(po =>
                    po.PoNumber == poNumber &&
                    po.PurchaseOrderId != id);

            if (duplicate)
            {
                return Conflict(new
                {
                    message = "Purchase order number already exists."
                });
            }

            purchaseOrder.PoNumber = poNumber;
            purchaseOrder.SupplierId = dto.SupplierId;
            purchaseOrder.OrderDate = dto.OrderDate;
            purchaseOrder.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Purchase order updated successfully.",
                purchaseOrder.PurchaseOrderId,
                purchaseOrder.PoNumber,
                purchaseOrder.SupplierId,
                purchaseOrder.OrderDate,
                purchaseOrder.ExpectedDeliveryDate,
                purchaseOrder.Status
            });
        }

        // POST: api/purchase-orders/1/lines
        [HttpPost("{id:long}/lines")]
        public async Task<IActionResult> AddLine(
            long id,
            [FromBody] CreatePurchaseOrderLineDto dto)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

            if (purchaseOrder == null)
            {
                return NotFound(new
                {
                    message = "Purchase order not found."
                });
            }

            if (purchaseOrder.Status != "DRAFT")
            {
                return BadRequest(new
                {
                    message = "Items can only be added to a draft purchase order."
                });
            }

            if (dto.OrderedQuantity <= 0)
            {
                return BadRequest(new
                {
                    message = "Ordered quantity must be greater than zero."
                });
            }

            if (dto.UnitCost.HasValue && dto.UnitCost.Value < 0)
            {
                return BadRequest(new
                {
                    message = "Unit cost cannot be negative."
                });
            }

            var itemExists = await _context.Items
                .AnyAsync(i => i.ItemId == dto.ItemId && i.IsActive);

            if (!itemExists)
            {
                return BadRequest(new
                {
                    message = "Item does not exist or is inactive."
                });
            }

            var uomExists = await _context.UnitsOfMeasures
                .AnyAsync(u => u.UomId == dto.UomId && u.IsActive);

            if (!uomExists)
            {
                return BadRequest(new
                {
                    message = "Unit of measure does not exist or is inactive."
                });
            }

            var line = new PurchaseOrderLine
            {
                PurchaseOrderId = id,
                ItemId = dto.ItemId,
                OrderedQuantity = dto.OrderedQuantity,
                UomId = dto.UomId,
                UnitCost = dto.UnitCost
            };

            _context.PurchaseOrderLines.Add(line);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Purchase order item added successfully.",
                line.PoLineId,
                line.PurchaseOrderId,
                line.ItemId,
                line.OrderedQuantity,
                line.UomId,
                line.UnitCost
            });
        }

        // PUT: api/purchase-orders/1/lines/1
        [HttpPut("{id:long}/lines/{lineId:long}")]
        public async Task<IActionResult> UpdateLine(
            long id,
            long lineId,
            [FromBody] UpdatePurchaseOrderLineDto dto)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

            if (purchaseOrder == null)
            {
                return NotFound(new
                {
                    message = "Purchase order not found."
                });
            }

            if (purchaseOrder.Status != "DRAFT")
            {
                return BadRequest(new
                {
                    message = "Only draft purchase order items can be updated."
                });
            }

            var line = await _context.PurchaseOrderLines
                .FirstOrDefaultAsync(line =>
                    line.PoLineId == lineId &&
                    line.PurchaseOrderId == id);

            if (line == null)
            {
                return NotFound(new
                {
                    message = "Purchase order item not found."
                });
            }

            if (dto.OrderedQuantity <= 0)
            {
                return BadRequest(new
                {
                    message = "Ordered quantity must be greater than zero."
                });
            }

            if (dto.UnitCost.HasValue && dto.UnitCost.Value < 0)
            {
                return BadRequest(new
                {
                    message = "Unit cost cannot be negative."
                });
            }

            var itemExists = await _context.Items
                .AnyAsync(i => i.ItemId == dto.ItemId && i.IsActive);

            if (!itemExists)
            {
                return BadRequest(new
                {
                    message = "Item does not exist or is inactive."
                });
            }

            var uomExists = await _context.UnitsOfMeasures
                .AnyAsync(u => u.UomId == dto.UomId && u.IsActive);

            if (!uomExists)
            {
                return BadRequest(new
                {
                    message = "Unit of measure does not exist or is inactive."
                });
            }

            line.ItemId = dto.ItemId;
            line.OrderedQuantity = dto.OrderedQuantity;
            line.UomId = dto.UomId;
            line.UnitCost = dto.UnitCost;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Purchase order item updated successfully.",
                line.PoLineId,
                line.PurchaseOrderId,
                line.ItemId,
                line.OrderedQuantity,
                line.UomId,
                line.UnitCost
            });
        }

        // DELETE: api/purchase-orders/1/lines/1
        [HttpDelete("{id:long}/lines/{lineId:long}")]
        public async Task<IActionResult> DeleteLine(
            long id,
            long lineId)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

            if (purchaseOrder == null)
            {
                return NotFound(new
                {
                    message = "Purchase order not found."
                });
            }

            if (purchaseOrder.Status != "DRAFT")
            {
                return BadRequest(new
                {
                    message = "Items can only be removed from a draft purchase order."
                });
            }

            var line = await _context.PurchaseOrderLines
                .FirstOrDefaultAsync(line =>
                    line.PoLineId == lineId &&
                    line.PurchaseOrderId == id);

            if (line == null)
            {
                return NotFound(new
                {
                    message = "Purchase order item not found."
                });
            }

            _context.PurchaseOrderLines.Remove(line);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Purchase order item removed successfully."
            });
        }
    }
}
