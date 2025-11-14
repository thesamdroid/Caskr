using MediatR;

namespace Caskr.Server.Events;

/// <summary>
///     Domain event that is emitted whenever an order transitions into a completed state.
/// </summary>
/// <param name="OrderId">The identifier of the completed order.</param>
/// <param name="CompanyId">Company that owns the order.</param>
/// <param name="InvoiceId">Invoice associated with the order, if any.</param>
public sealed record OrderCompletedEvent(int OrderId, int CompanyId, int? InvoiceId) : INotification;
