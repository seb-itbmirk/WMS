using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class ItemBarcode
{
    public long BarcodeId { get; set; }

    public long ItemId { get; set; }

    public string BarcodeValue { get; set; } = null!;

    public string BarcodeType { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Item Item { get; set; } = null!;
}
