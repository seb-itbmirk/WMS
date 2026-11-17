using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class WarehouseZone
{
    public long ZoneId { get; set; }

    public long WarehouseId { get; set; }

    public string ZoneCode { get; set; } = null!;

    public string ZoneName { get; set; } = null!;

    public virtual ICollection<StorageBin> StorageBins { get; set; } = new List<StorageBin>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
