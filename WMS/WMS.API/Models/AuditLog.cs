using System;
using System.Collections.Generic;
using System.Net;

namespace WMS.API.Models;

public partial class AuditLog
{
    public long AuditId { get; set; }

    public long? UserId { get; set; }

    public string TableName { get; set; } = null!;

    public string RecordId { get; set; } = null!;

    public string ActionType { get; set; } = null!;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public DateTime ActionTimestamp { get; set; }

    public IPAddress? IpAddress { get; set; }

    public virtual AppUser? User { get; set; }
}
