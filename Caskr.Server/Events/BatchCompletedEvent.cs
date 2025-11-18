using MediatR;

namespace Caskr.Server.Events;

/// <summary>
///     Raised when a batch transitions into a completed state so downstream systems
///     (QuickBooks, analytics, etc.) can react without blocking the original workflow.
/// </summary>
/// <param name="BatchId">Identifier of the batch that finished processing.</param>
/// <param name="CompanyId">Company that owns the batch.</param>
public sealed record BatchCompletedEvent(int BatchId, int CompanyId) : INotification;
