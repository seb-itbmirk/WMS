
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS.API.Data;

namespace WMS.API.Controllers;

[Authorize(Policy = "Permission:inventory.view")]
[Route("api/inventory-balances")]
[ApiController]
public class InventoryBalancesController : ControllerBase
{
    private readonly WmsDbContext _context;

    public InventoryBalancesController(WmsDbContext context)
    {
        _context = context;
    }

    // Get authenticated user's ID from JWT.
    private long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }

    // Get warehouses assigned to the authenticated user.
    private async Task<List<long>> GetAccessibleWarehouseIdsAsync(
        long userId)
    {
        return await _context.Database
            .SqlQuery<long>($"""
                SELECT warehouse_id AS "Value"
                FROM user_warehouse_access
                WHERE user_id = {userId}
                """)
            .ToListAsync();
    }

    // GET: api/inventory-balances
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var warehouseIds =
            await GetAccessibleWarehouseIdsAsync(userId.Value);

        var balances = await _context.InventoryBalances
            .AsNoTracking()
            .Where(b =>
                warehouseIds.Contains(b.Bin.Zone.WarehouseId))
            .OrderBy(b => b.Lot.Item.ItemCode)
            .ThenBy(b => b.Lot.ReceivedDate)
            .Select(b => new
            {
                b.BalanceId,
                b.LotId,
                b.Lot.BatchCode,
                b.Lot.SupplierBatchCode,
                b.Lot.ExpiryDate,

                b.Lot.ItemId,
                ItemCode = b.Lot.Item.ItemCode,
                ItemName = b.Lot.Item.ItemName,

                b.BinId,
                BinCode = b.Bin.BinCode,

                WarehouseId = b.Bin.Zone.WarehouseId,
                WarehouseCode =
                    b.Bin.Zone.Warehouse.WarehouseCode,
                WarehouseName =
                    b.Bin.Zone.Warehouse.WarehouseName,

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
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var warehouseIds =
            await GetAccessibleWarehouseIdsAsync(userId.Value);

        var balance = await _context.InventoryBalances
            .AsNoTracking()
            .Where(b =>
                b.BalanceId == id &&
                warehouseIds.Contains(b.Bin.Zone.WarehouseId))
            .Select(b => new
            {
                b.BalanceId,
                b.LotId,
                b.Lot.BatchCode,
                b.Lot.SupplierBatchCode,
                b.Lot.ExpiryDate,

                b.Lot.ItemId,
                ItemCode = b.Lot.Item.ItemCode,
                ItemName = b.Lot.Item.ItemName,

                b.BinId,
                BinCode = b.Bin.BinCode,

                WarehouseId = b.Bin.Zone.WarehouseId,
                WarehouseCode =
                    b.Bin.Zone.Warehouse.WarehouseCode,
                WarehouseName =
                    b.Bin.Zone.Warehouse.WarehouseName,

                b.StockStatus,
                b.QuantityOnHand,
                b.QuantityReserved,

                AvailableQuantity =
                    b.QuantityOnHand - b.QuantityReserved,

                b.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (balance == null)
        {
            return NotFound(new
            {
                message = "Inventory balance not found."
            });
        }

        return Ok(balance);
    }

    // GET: api/inventory-balances/item/1
    [HttpGet("item/{itemId:long}")]
    public async Task<IActionResult> GetByItem(long itemId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var itemExists = await _context.Items
            .AsNoTracking()
            .AnyAsync(i => i.ItemId == itemId);

        if (!itemExists)
        {
            return NotFound(new
            {
                message = "Item not found."
            });
        }

        var warehouseIds =
            await GetAccessibleWarehouseIdsAsync(userId.Value);

        var balances = await _context.InventoryBalances
            .AsNoTracking()
            .Where(b =>
                b.Lot.ItemId == itemId &&
                warehouseIds.Contains(b.Bin.Zone.WarehouseId))
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

                WarehouseId = b.Bin.Zone.WarehouseId,

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
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var warehouseIds =
            await GetAccessibleWarehouseIdsAsync(userId.Value);

        // Only find the bin if it belongs to an accessible warehouse.
        var accessibleBin = await _context.StorageBins
            .AsNoTracking()
            .Where(b =>
                b.BinId == binId &&
                warehouseIds.Contains(b.Zone.WarehouseId))
            .Select(b => new
            {
                b.BinId,
                b.Zone.WarehouseId
            })
            .FirstOrDefaultAsync();

        if (accessibleBin == null)
        {
            return NotFound(new
            {
                message = "Storage bin not found."
            });
        }

        var balances = await _context.InventoryBalances
            .AsNoTracking()
            .Where(b =>
                b.BinId == binId &&
                warehouseIds.Contains(b.Bin.Zone.WarehouseId))
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

                WarehouseId = b.Bin.Zone.WarehouseId,

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
