using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class ProductionMaterialIssue
{
    public long IssueId { get; set; }

    public string IssueNumber { get; set; } = null!;

    public long ProductionOrderId { get; set; }

    public long WarehouseId { get; set; }

    public DateTime IssueDate { get; set; }

    public string Status { get; set; } = null!;

    public long? IssuedBy { get; set; }

    public virtual AppUser? IssuedByNavigation { get; set; }

    public virtual ICollection<ProductionMaterialIssueLine> ProductionMaterialIssueLines { get; set; } = new List<ProductionMaterialIssueLine>();

    public virtual ProductionOrder ProductionOrder { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
