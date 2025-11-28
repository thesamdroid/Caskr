using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Models;
using Intuit.Ipp.Core;
using Intuit.Ipp.Exception;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InvoiceModel = Caskr.server.Models.Invoice;
using QuickBooksInvoice = Intuit.Ipp.Data.Invoice;
using IppCustomer = Intuit.Ipp.Data.Customer;
using IppEmailAddress = Intuit.Ipp.Data.EmailAddress;
using IppTelephoneNumber = Intuit.Ipp.Data.TelephoneNumber;
using IppPhysicalAddress = Intuit.Ipp.Data.PhysicalAddress;
using IppReferenceType = Intuit.Ipp.Data.ReferenceType;
using IppLine = Intuit.Ipp.Data.Line;
using IppLineDetailTypeEnum = Intuit.Ipp.Data.LineDetailTypeEnum;
using IppSalesItemLineDetail = Intuit.Ipp.Data.SalesItemLineDetail;
using IppTxnTaxDetail = Intuit.Ipp.Data.TxnTaxDetail;
using IppTaxLineDetail = Intuit.Ipp.Data.TaxLineDetail;

namespace Caskr.Server.Services;

/// <summary>
///     Synchronizes invoices from Caskr into QuickBooks Online. The implementation handles customer creation,
///     invoice mapping, retry logic for transient failures, and persisting the results to <c>accounting_sync_logs</c>.
/// </summary>
public class QuickBooksInvoiceSyncService : IQuickBooksInvoiceSyncService
{
    private readonly CaskrDbContext _dbContext;
    private readonly IQuickBooksIntegrationContextFactory _integrationContextFactory;
    private readonly IQuickBooksInvoiceClient _quickBooksClient;
    private readonly IQuickBooksSyncLogService _syncLogService;
    private readonly IQuickBooksAccountMappingService _accountMappingService;
    private readonly ILogger<QuickBooksInvoiceSyncService> _logger;

    public QuickBooksInvoiceSyncService(
        CaskrDbContext dbContext,
        IQuickBooksIntegrationContextFactory integrationContextFactory,
        IQuickBooksInvoiceClient quickBooksClient,
        IQuickBooksSyncLogService syncLogService,
        IQuickBooksAccountMappingService accountMappingService,
        ILogger<QuickBooksInvoiceSyncService> logger)
    {
        _dbContext = dbContext;
        _integrationContextFactory = integrationContextFactory;
        _quickBooksClient = quickBooksClient;
        _syncLogService = syncLogService;
        _accountMappingService = accountMappingService;
        _logger = logger;
    }

    public async Task<InvoiceSyncResult> SyncInvoiceToQBOAsync(int invoiceId)
    {
        if (invoiceId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(invoiceId));
        }

        var invoice = await LoadInvoiceAsync(invoiceId);
        if (invoice is null)
        {
            return new InvoiceSyncResult(false, null, $"Invoice {invoiceId} was not found.");
        }

        var invoiceIdText = invoiceId.ToString(CultureInfo.InvariantCulture);
        var existingQboId = await _syncLogService.GetSuccessfulSyncExternalIdAsync(
            invoice.CompanyId,
            QuickBooksConstants.EntityTypes.Invoice,
            invoiceIdText);

        if (existingQboId is not null)
        {
            _logger.LogInformation("Invoice {InvoiceId} already synced to QuickBooks with QBO Id {QboInvoiceId}",
                invoiceId, existingQboId);
            return new InvoiceSyncResult(true, existingQboId, null);
        }

        var syncLog = await _syncLogService.GetOrCreateSyncLogAsync(
            invoice.CompanyId,
            QuickBooksConstants.EntityTypes.Invoice,
            invoiceIdText);
        _syncLogService.UpdateSyncLog(syncLog, SyncStatus.InProgress, null, syncLog.ExternalEntityId);
        await _dbContext.SaveChangesAsync();

        var attempt = 0;
        var delay = QuickBooksConstants.RetryPolicy.InitialRetryDelay;
        Exception? lastTransientException = null;

        while (attempt < QuickBooksConstants.RetryPolicy.MaxRetryCount)
        {
            attempt++;
            try
            {
                var integrationContext = await _integrationContextFactory.CreateAsync(invoice.CompanyId);
                var serviceContext = integrationContext.ServiceContext;
                var accountMappings = await _accountMappingService.LoadAccountMappingsAsync(invoice.CompanyId);
                var customer = await EnsureCustomerAsync(serviceContext, invoice, CancellationToken.None);
                var qbInvoice = MapInvoice(invoice, customer, accountMappings);
                var createdInvoice = await _quickBooksClient.CreateInvoiceAsync(serviceContext, qbInvoice, CancellationToken.None);

                _syncLogService.UpdateSyncLog(syncLog, SyncStatus.Success, null, createdInvoice.Id);
                syncLog.RetryCount = attempt - 1;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully synced invoice {InvoiceId} to QuickBooks with Id {QboInvoiceId}",
                    invoiceId, createdInvoice.Id);

                return new InvoiceSyncResult(true, createdInvoice.Id, null);
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < QuickBooksConstants.RetryPolicy.MaxRetryCount)
            {
                lastTransientException = ex;
                syncLog.RetryCount = attempt;
                _syncLogService.UpdateSyncLog(syncLog, SyncStatus.InProgress, ex.Message, syncLog.ExternalEntityId);
                await _dbContext.SaveChangesAsync();

                _logger.LogWarning(ex,
                    "Transient QuickBooks error while syncing invoice {InvoiceId}. Retrying attempt {Attempt}/{Max}",
                    invoiceId, attempt, QuickBooksConstants.RetryPolicy.MaxRetryCount);

                await Task.Delay(delay);
                delay = TimeSpan.FromSeconds(delay.TotalSeconds * QuickBooksConstants.RetryPolicy.BackoffMultiplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync invoice {InvoiceId} to QuickBooks", invoiceId);
                _syncLogService.UpdateSyncLog(syncLog, SyncStatus.Failed, ex.Message, syncLog.ExternalEntityId);
                syncLog.RetryCount = attempt;
                await _dbContext.SaveChangesAsync();
                return new InvoiceSyncResult(false, null, ex.Message);
            }
        }

        var message = lastTransientException?.Message ?? "QuickBooks sync failed after multiple attempts.";
        _syncLogService.UpdateSyncLog(syncLog, SyncStatus.Failed, message, syncLog.ExternalEntityId);
        await _dbContext.SaveChangesAsync();
        return new InvoiceSyncResult(false, null, message);
    }

    public Task<PaymentSyncResult> SyncPaymentToQBOAsync(int paymentId)
    {
        _logger.LogWarning("Payment sync invoked for payment {PaymentId}, but the feature is not implemented yet.", paymentId);
        return Task.FromResult(new PaymentSyncResult(false, null, "Payment sync is not implemented yet."));
    }

    public async Task<BatchSyncResult> SyncAllPendingAsync(int companyId)
    {
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId));
        }

        var pendingInvoiceIds = await _dbContext.AccountingSyncLogs
            .AsNoTracking()
            .Where(log => log.CompanyId == companyId
                          && log.EntityType == QuickBooksConstants.EntityTypes.Invoice
                          && (log.SyncStatus == SyncStatus.Pending || log.SyncStatus == SyncStatus.Failed))
            .Select(log => log.EntityId)
            .ToListAsync();

        var successCount = 0;
        var failureCount = 0;
        var errors = new List<string>();

        foreach (var entityId in pendingInvoiceIds)
        {
            if (!int.TryParse(entityId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var invoiceId))
            {
                continue;
            }

            var result = await SyncInvoiceToQBOAsync(invoiceId);
            if (result.Success)
            {
                successCount++;
            }
            else
            {
                failureCount++;
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    errors.Add($"Invoice {invoiceId}: {result.ErrorMessage}");
                }
            }
        }

        return new BatchSyncResult(successCount, failureCount, errors);
    }

    private async Task<InvoiceModel?> LoadInvoiceAsync(int invoiceId)
    {
        return await _dbContext.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Taxes)
            .SingleOrDefaultAsync(i => i.Id == invoiceId);
    }

    private async Task<IppCustomer> EnsureCustomerAsync(ServiceContext serviceContext, InvoiceModel invoice, CancellationToken cancellationToken)
    {
        IppCustomer? customer = null;

        if (!string.IsNullOrWhiteSpace(invoice.CustomerEmail))
        {
            customer = await _quickBooksClient.FindCustomerByEmailAsync(serviceContext, invoice.CustomerEmail, cancellationToken);
        }

        if (customer is null)
        {
            customer = await _quickBooksClient.FindCustomerByDisplayNameAsync(serviceContext, invoice.CustomerName, cancellationToken);
        }

        if (customer is not null)
        {
            return customer;
        }

        var qbCustomer = new IppCustomer
        {
            DisplayName = invoice.CustomerName,
            GivenName = invoice.CustomerName,
            PrimaryEmailAddr = string.IsNullOrWhiteSpace(invoice.CustomerEmail)
                ? null
                : new IppEmailAddress { Address = invoice.CustomerEmail },
            PrimaryPhone = string.IsNullOrWhiteSpace(invoice.CustomerPhone)
                ? null
                : new IppTelephoneNumber { FreeFormNumber = invoice.CustomerPhone },
            BillAddr = new IppPhysicalAddress
            {
                Line1 = invoice.CustomerAddressLine1,
                Line2 = invoice.CustomerAddressLine2,
                City = invoice.CustomerCity,
                CountrySubDivisionCode = invoice.CustomerState,
                PostalCode = invoice.CustomerPostalCode,
                Country = invoice.CustomerCountry
            }
        };

        return await _quickBooksClient.CreateCustomerAsync(serviceContext, qbCustomer, cancellationToken);
    }

    private static QuickBooksInvoice MapInvoice(
        InvoiceModel invoice,
        IppCustomer customer,
        IReadOnlyDictionary<CaskrAccountType, ChartOfAccountsMapping> accountMappings)
    {
        if (invoice.LineItems.Count == 0)
        {
            throw new InvalidOperationException($"Invoice {invoice.Id} does not have any line items.");
        }

        var qbInvoice = new QuickBooksInvoice
        {
            CustomerRef = new IppReferenceType { Value = customer.Id ?? customer.DisplayName, name = customer.DisplayName },
            DocNumber = invoice.InvoiceNumber,
            TxnDate = invoice.InvoiceDate,
            TxnDateSpecified = true,
            PrivateNote = invoice.Notes,
            BillEmail = string.IsNullOrWhiteSpace(invoice.CustomerEmail)
                ? null
                : new IppEmailAddress { Address = invoice.CustomerEmail },
            BillAddr = new IppPhysicalAddress
            {
                Line1 = invoice.CustomerAddressLine1,
                Line2 = invoice.CustomerAddressLine2,
                City = invoice.CustomerCity,
                CountrySubDivisionCode = invoice.CustomerState,
                Country = invoice.CustomerCountry,
                PostalCode = invoice.CustomerPostalCode
            },
            CurrencyRef = new IppReferenceType { Value = invoice.CurrencyCode }
        };

        if (invoice.DueDate.HasValue)
        {
            qbInvoice.DueDate = invoice.DueDate.Value;
            qbInvoice.DueDateSpecified = true;
        }

        qbInvoice.Line = invoice.LineItems
            .Select(item => MapLineItem(item, accountMappings))
            .ToArray();

        qbInvoice.TxnTaxDetail = MapTaxes(invoice);
        return qbInvoice;
    }

    private static IppLine MapLineItem(
        InvoiceLineItem lineItem,
        IReadOnlyDictionary<CaskrAccountType, ChartOfAccountsMapping> accountMappings)
    {
        if (!accountMappings.TryGetValue(lineItem.AccountType, out var mapping))
        {
            throw new InvalidOperationException($"Missing chart of accounts mapping for account type {lineItem.AccountType}.");
        }

        var amount = Math.Round(lineItem.Quantity * lineItem.UnitPrice, 2, MidpointRounding.AwayFromZero);
        var salesItemDetail = new IppSalesItemLineDetail
        {
            ItemRef = new IppReferenceType { Value = mapping.QboAccountId, name = mapping.QboAccountName },
            Qty = lineItem.Quantity,
            TaxCodeRef = new IppReferenceType
            {
                Value = lineItem.IsTaxable
                    ? QuickBooksConstants.TaxCodes.Taxable
                    : QuickBooksConstants.TaxCodes.NonTaxable
            }
        };

        return new IppLine
        {
            Description = lineItem.Description,
            Amount = amount,
            DetailType = IppLineDetailTypeEnum.SalesItemLineDetail,
            AnyIntuitObject = salesItemDetail
        };
    }

    private static IppTxnTaxDetail? MapTaxes(InvoiceModel invoice)
    {
        if (invoice.Taxes.Count == 0)
        {
            return null;
        }

        var totalTax = invoice.Taxes.Sum(t => t.Amount);
        var taxLines = invoice.Taxes.Select(tax =>
        {
            var detail = new IppTaxLineDetail
            {
                TaxRateRef = new IppReferenceType { Value = tax.TaxCode },
                PercentBased = true,
                TaxPercent = tax.Rate
            };

            return new IppLine
            {
                Amount = tax.Amount,
                DetailType = IppLineDetailTypeEnum.TaxLineDetail,
                AnyIntuitObject = detail
            };
        }).ToArray();

        return new IppTxnTaxDetail
        {
            TotalTax = totalTax,
            TaxLine = taxLines,
            TxnTaxCodeRef = new IppReferenceType { Value = invoice.Taxes.First().TaxCode }
        };
    }

    private static bool IsTransient(Exception ex)
    {
        return ex switch
        {
            IdsException idsException => string.Equals(idsException.ErrorCode, "429", StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(idsException.ErrorCode, "500", StringComparison.OrdinalIgnoreCase),
            HttpRequestException => true,
            TaskCanceledException => true,
            TimeoutException => true,
            _ => false
        };
    }
}
