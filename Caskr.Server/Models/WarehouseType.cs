namespace Caskr.server.Models;

/// <summary>
/// Types of warehouse storage facilities for barrel inventory
/// </summary>
public enum WarehouseType
{
    /// <summary>Traditional barrel rack house with tiered storage</summary>
    Rickhouse,

    /// <summary>Modern palletized warehouse with forklift access</summary>
    Palletized,

    /// <summary>Tank storage facility for bulk spirits</summary>
    Tank_Farm,

    /// <summary>Outdoor barrel storage area</summary>
    Outdoor
}
