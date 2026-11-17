
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;

namespace WMS.API.Controllers
{
    [Route("api/inventory-balances")]
    [ApiController]
    public class InventoryBalancesController : ControllerBase
    {
        private readonly WmsDbContext _context;

        public InventoryBalancesController(WmsDbContext context)
        {
            _context = context;
        }

        // GET: api/inventory-balances
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var balances = await _context.InventoryBalances
                .OrderBy(b => b.Lot.Item.ItemCode)
                .ThenBy(b => b.Lot.ReceivedDate)
                .Select(b => new
                {
                    b.BalanceId,

                    b.LotId,
                    b.Lot.BatchCode,
                    b.Lot.SupplierBatchCode,

                    b.Lot.ItemId,
                    ItemCode = b.Lot.Item.ItemCode,
                    ItemName = b.Lot.Item.ItemName,

                    b.BinId,
                    BinCode = b.Bin.BinCode,

                    b.StockStatus,
                    b.QuantityOnHand,
                    b.QuantityReserved,

                    AvailableQuantity =
                        b.QuantityOnHand - b.QuantityReserved,

                    b.UpdatedAt
                })
                .ToListAsync();

            return Ok(balances);
        }

        // GET: api/inventory-balances/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var balance = await _context.InventoryBalances
                .Where(b => b.BalanceId == id)
                .Select(b => new
                {
                    b.BalanceId,

                    b.LotId,
                    b.Lot.BatchCode,

                    b.Lot.ItemId,
                    ItemCode = b.Lot.Item.ItemCode,
                    ItemName = b.Lot.Item.ItemName,

                    b.BinId,
                    BinCode = b.Bin.BinCode,

                    b.StockStatus,
                    b.QuantityOnHand,
                    b.QuantityReserved,

                    AvailableQuantity =
                        b.QuantityOnHand - b.QuantityReserved,

                    b.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (balance == null)
                return NotFound("Inventory balance not found.");

            return Ok(balance);
        }

        // GET: api/inventory-balances/item/1
        [HttpGet("item/{itemId:long}")]
        public async Task<IActionResult> GetByItem(long itemId)
        {
            var itemExists = await _context.Items
                .AnyAsync(i => i.ItemId == itemId);

            if (!itemExists)
                return NotFound("Item not found.");

            var balances = await _context.InventoryBalances
                .Where(b => b.Lot.ItemId == itemId)
                .OrderBy(b => b.Lot.ReceivedDate)
                .Select(b => new
                {
                    b.BalanceId,

                    b.LotId,
                    b.Lot.BatchCode,
                    b.Lot.ExpiryDate,

                    b.Lot.ItemId,
                    ItemCode = b.Lot.Item.ItemCode,
                    ItemName = b.Lot.Item.ItemName,

                    b.BinId,
                    BinCode = b.Bin.BinCode,

                    b.StockStatus,
                    b.QuantityOnHand,
                    b.QuantityReserved,

                    AvailableQuantity =
                        b.QuantityOnHand - b.QuantityReserved,

                    b.UpdatedAt
                })
                .ToListAsync();

            return Ok(balances);
        }

        // GET: api/inventory-balances/bin/1
        [HttpGet("bin/{binId:long}")]
        public async Task<IActionResult> GetByBin(long binId)
        {
            var binExists = await _context.StorageBins
                .AnyAsync(b => b.BinId == binId);

            if (!binExists)
                return NotFound("Storage bin not found.");

            var balances = await _context.InventoryBalances
                .Where(b => b.BinId == binId)
                .OrderBy(b => b.Lot.Item.ItemCode)
                .Select(b => new
                {
                    b.BalanceId,

                    b.LotId,
                    b.Lot.BatchCode,

                    b.Lot.ItemId,
                    ItemCode = b.Lot.Item.ItemCode,
                    ItemName = b.Lot.Item.ItemName,

                    b.BinId,
                    BinCode = b.Bin.BinCode,

                    b.StockStatus,
                    b.QuantityOnHand,
                    b.QuantityReserved,

                    AvailableQuantity =
                        b.QuantityOnHand - b.QuantityReserved,

                    b.UpdatedAt
                })
                .ToListAsync();

            return Ok(balances);
        }
    }
}
