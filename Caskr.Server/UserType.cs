namespace Caskr.server;

/// <summary>
/// Well-known user types.
/// </summary>
public enum UserType
{
    /// <summary>
    /// Super administrator user type identifier.
    /// </summary>
    SuperAdmin = 1,

    /// <summary>
    /// Administrator user type identifier.
    /// </summary>
    Admin = 2,

    /// <summary>
    /// Distiller user type identifier.
    /// </summary>
    Distiller = 3,

    /// <summary>
    /// Distributor user type identifier.
    /// </summary>
    Distributor = 4,

    /// <summary>
    /// Retailer user type identifier.
    /// </summary>
    Retailer = 5,

    /// <summary>
    /// Compliance Manager user type identifier for TTB regulatory approvals.
    /// </summary>
    ComplianceManager = 6
}

