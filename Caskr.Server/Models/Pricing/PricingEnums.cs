namespace Caskr.server.Models.Pricing;

/// <summary>
/// Types of discounts that can be applied by promotions.
/// </summary>
public enum DiscountType
{
    /// <summary>
    /// Percentage discount off the original price (e.g., 20 for 20% off).
    /// </summary>
    Percentage = 0,

    /// <summary>
    /// Fixed amount discount in cents (e.g., 5000 for $50 off).
    /// </summary>
    FixedAmount = 1,

    /// <summary>
    /// Free months of service (e.g., 2 for 2 free months).
    /// </summary>
    FreeMonths = 2
}

/// <summary>
/// Actions that can be performed on pricing entities for audit logging.
/// </summary>
public enum PricingAuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    Activate = 3,
    Deactivate = 4
}
