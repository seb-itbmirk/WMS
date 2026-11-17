using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class LotBarcode
{
    public long LotBarcodeId { get; set; }

    public long LotId { get; set; }

    public string BarcodeValue { get; set; } = null!;

    public string BarcodeType { get; set; } = null!;

    public virtual InventoryLot Lot { get; set; } = null!;
}
