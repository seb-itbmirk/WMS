using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class ProductionBatchGenealogy
{
    public long GenealogyId { get; set; }

    public long ProductionOrderId { get; set; }

    public long InputLotId { get; set; }

    public long OutputLotId { get; set; }

    public decimal ConsumedQuantity { get; set; }

    public decimal? ProducedQuantity { get; set; }

    public long UomId { get; set; }

    public virtual InventoryLot InputLot { get; set; } = null!;

    public virtual InventoryLot OutputLot { get; set; } = null!;

    public virtual ProductionOrder ProductionOrder { get; set; } = null!;

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
