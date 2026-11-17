using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class ProductionOrder
{
    public long ProductionOrderId { get; set; }

    public string ProductionNumber { get; set; } = null!;

    public long BomId { get; set; }

    public long WarehouseId { get; set; }

    public long FinishedItemId { get; set; }

    public decimal PlannedQuantity { get; set; }

    public long UomId { get; set; }

    public DateOnly? PlannedStartDate { get; set; }

    public DateOnly? PlannedEndDate { get; set; }

    public DateTime? ActualStartDate { get; set; }

    public DateTime? ActualEndDate { get; set; }

    public string ProductionStatus { get; set; } = null!;

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual BillOfMaterial Bom { get; set; } = null!;

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual Item FinishedItem { get; set; } = null!;

    public virtual ICollection<ProductionBatchGenealogy> ProductionBatchGenealogies { get; set; } = new List<ProductionBatchGenealogy>();

    public virtual ICollection<ProductionConsumption> ProductionConsumptions { get; set; } = new List<ProductionConsumption>();

    public virtual ICollection<ProductionMaterialIssue> ProductionMaterialIssues { get; set; } = new List<ProductionMaterialIssue>();

    public virtual ICollection<ProductionMaterialRequirement> ProductionMaterialRequirements { get; set; } = new List<ProductionMaterialRequirement>();

    public virtual ICollection<ProductionOutput> ProductionOutputs { get; set; } = new List<ProductionOutput>();

    public virtual ICollection<ProductionWaste> ProductionWastes { get; set; } = new List<ProductionWaste>();

    public virtual ICollection<QualityInspection> QualityInspections { get; set; } = new List<QualityInspection>();

    public virtual UnitsOfMeasure Uom { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
