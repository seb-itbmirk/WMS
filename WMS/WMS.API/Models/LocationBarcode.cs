using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class LocationBarcode
{
    public long LocationBarcodeId { get; set; }

    public long BinId { get; set; }

    public string BarcodeValue { get; set; } = null!;

    public string BarcodeType { get; set; } = null!;

    public virtual StorageBin Bin { get; set; } = null!;
}
