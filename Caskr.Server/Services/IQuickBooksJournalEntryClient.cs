using System;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server;
using Intuit.Ipp.Core;
using Intuit.Ipp.DataService;
using Microsoft.Extensions.Logging;
using QuickBooksJournalEntry = Intuit.Ipp.Data.JournalEntry;

namespace Caskr.Server.Services;

/// <summary>
///     Thin abstraction over the Intuit SDK for creating QuickBooks journal entries. Abstracting the
///     SDK keeps <see cref="QuickBooksCostTrackingService" /> unit-testable.
/// </summary>
public interface IQuickBooksJournalEntryClient
{
    Task<QuickBooksJournalEntry> CreateJournalEntryAsync(ServiceContext context, QuickBooksJournalEntry journalEntry, CancellationToken cancellationToken);
}

[AutoBind]
public sealed class QuickBooksJournalEntryClient : IQuickBooksJournalEntryClient
{
    private readonly ILogger<QuickBooksJournalEntryClient> _logger;

    public QuickBooksJournalEntryClient(ILogger<QuickBooksJournalEntryClient> logger)
    {
        _logger = logger;
    }

    public Task<QuickBooksJournalEntry> CreateJournalEntryAsync(ServiceContext context, QuickBooksJournalEntry journalEntry, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (journalEntry is null)
        {
            throw new ArgumentNullException(nameof(journalEntry));
        }

        return Task.Run(() =>
        {
            _logger.LogInformation("Creating QuickBooks journal entry {DocNumber}", journalEntry.DocNumber ?? journalEntry.PrivateNote);
            var dataService = new DataService(context);
            return dataService.Add(journalEntry);
        }, cancellationToken);
    }
}
