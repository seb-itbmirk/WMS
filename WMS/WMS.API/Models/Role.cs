using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class Role
{
    public long RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
