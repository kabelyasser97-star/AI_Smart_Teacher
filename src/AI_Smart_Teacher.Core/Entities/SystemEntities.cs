using AI_Smart_Teacher.Core.Enums;

namespace AI_Smart_Teacher.Core.Entities;

public class SubscriptionPlan : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int DurationDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Subscription : EntityBase
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
}

public class PaymentRecord : EntityBase
{
    public Guid SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Provider { get; set; } = "Manual";
    public string Status { get; set; } = "Pending";
    public string? ExternalReference { get; set; }
}

public class Notification : EntityBase
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}

public class SystemSetting : EntityBase
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Category { get; set; }
    public bool IsSensitive { get; set; }
}

public class AuditLog : EntityBase
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public bool IsAdministrative { get; set; }
}
