using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class SystemNotification
{
    public long NotificationId { get; set; }

    public long? UserId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AppUser? User { get; set; }
}
