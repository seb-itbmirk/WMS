using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class ItemCategory
{
    public long CategoryId { get; set; }

    public string CategoryCode { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
