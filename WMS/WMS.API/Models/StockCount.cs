using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class StockCount
{
    public long StockCountId { get; set; }

    public string CountNumber { get; set; } = null!;

    public long WarehouseId { get; set; }

    public DateTime CountDate { get; set; }

    public string Status { get; set; } = null!;

    public long? CreatedBy { get; set; }

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual ICollection<StockCountLine> StockCountLines { get; set; } = new List<StockCountLine>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
