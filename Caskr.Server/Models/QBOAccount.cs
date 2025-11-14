namespace Caskr.Server.Models;

/// <summary>
///     Lightweight representation of a QuickBooks Online account returned by the chart of accounts query.
/// </summary>
public sealed record QBOAccount(
    string Id,
    string Name,
    string AccountType,
    string? AccountSubType,
    bool Active);
