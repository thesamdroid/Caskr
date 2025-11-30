using Caskr.server.Models;
using Intuit.Ipp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Caskr.server.Services;

/// <summary>
/// Service for calculating and tracking federal excise tax on removed spirits
/// </summary>
public interface ITtbExciseTaxService
{
    /// <summary>
    /// Calculates federal excise tax for an order based on removed spirits
    /// </summary>
    Task<ExciseTaxCalculation> CalculateTaxAsync(int orderId);

    /// <summary>
    /// Records a tax determination in the database and creates TtbTransaction
    /// </summary>
    Task<TtbTaxDetermination> RecordTaxDeterminationAsync(int orderId, ExciseTaxCalculation calculation);

    /// <summary>
    /// Posts excise tax liability to QuickBooks if integration is enabled
    /// </summary>
    Task<bool> PostTaxLiabilityToQuickBooksAsync(int taxDeterminationId);

    /// <summary>
    /// Generates an excise tax report for a given month and year
    /// </summary>
    Task<ExciseTaxReport> GenerateExciseTaxReportAsync(int companyId, int month, int year);
}

[AutoBind]
public class TtbExciseTaxService : ITtbExciseTaxService
{
    // Federal excise tax rates per proof gallon (26 U.S.C. § 5001)
    private const decimal StandardTaxRate = 13.50m;
    private const decimal ReducedTaxRate = 13.34m;
    private const decimal ReducedRateThresholdProofGallons = 100000m;
    private const decimal AnnualProductionEligibilityLimit = 2250000m;

    private readonly CaskrDbContext _dbContext;
    private readonly IQuickBooksIntegrationContextFactory _integrationContextFactory;
    private readonly IQuickBooksJournalEntryClient _journalEntryClient;
    private readonly IQuickBooksSyncLogService _syncLogService;
    private readonly IQuickBooksAccountMappingService _accountMappingService;
    private readonly ILogger<TtbExciseTaxService> _logger;

    public TtbExciseTaxService(
        CaskrDbContext dbContext,
        IQuickBooksIntegrationContextFactory integrationContextFactory,
        IQuickBooksJournalEntryClient journalEntryClient,
        IQuickBooksSyncLogService syncLogService,
        IQuickBooksAccountMappingService accountMappingService,
        ILogger<TtbExciseTaxService> logger)
    {
        _dbContext = dbContext;
        _integrationContextFactory = integrationContextFactory;
        _journalEntryClient = journalEntryClient;
        _syncLogService = syncLogService;
        _accountMappingService = accountMappingService;
        _logger = logger;
    }

    public async Task<ExciseTaxCalculation> CalculateTaxAsync(int orderId)
    {
        _logger.LogInformation("Calculating excise tax for order {OrderId}", orderId);

        var order = await _dbContext.Orders
            .Include(o => o.Barrels)
            .Include(o => o.SpiritType)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException($"Order {orderId} was not found.");

        // Get all gauge records for barrels in this order (removal gauges)
        var barrelIds = order.Barrels.Select(b => b.Id).ToList();
        var removalGauges = await _dbContext.TtbGaugeRecords
            .Where(gr => barrelIds.Contains(gr.BarrelId) && gr.GaugeType == TtbGaugeType.Removal)
            .AsNoTracking()
            .ToListAsync();

        // Sum total proof gallons removed
        var totalProofGallons = removalGauges.Sum(gr => gr.ProofGallons);

        if (totalProofGallons <= 0)
        {
            throw new InvalidOperationException($"Order {orderId} has no proof gallons recorded for removal.");
        }

        // Determine company eligibility for reduced rate
        var company = await _dbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == order.CompanyId)
            ?? throw new InvalidOperationException($"Company {order.CompanyId} was not found.");

        var eligibilityResult = DetermineEligibility(company);
        var isEligible = eligibilityResult.IsEligible;
        var eligibilityReason = eligibilityResult.Reason;

        // Calculate tax with graduated rates
        decimal proofGallonsAtReducedRate = 0m;
        decimal proofGallonsAtStandardRate = 0m;
        decimal reducedRateTax = 0m;
        decimal standardRateTax = 0m;

        if (isEligible && totalProofGallons > 0)
        {
            // Get year-to-date proof gallons for the company (current calendar year)
            var currentYear = DateTime.UtcNow.Year;
            var yearStart = new DateTime(currentYear, 1, 1);
            var ytdProofGallons = await GetYearToDateProofGallonsAsync(order.CompanyId, yearStart);

            // Determine how much of current order qualifies for reduced rate
            var remainingReducedRateCapacity = Math.Max(0, ReducedRateThresholdProofGallons - ytdProofGallons);
            proofGallonsAtReducedRate = Math.Min(totalProofGallons, remainingReducedRateCapacity);
            proofGallonsAtStandardRate = totalProofGallons - proofGallonsAtReducedRate;

            reducedRateTax = proofGallonsAtReducedRate * ReducedTaxRate;
            standardRateTax = proofGallonsAtStandardRate * StandardTaxRate;
        }
        else
        {
            // Not eligible for reduced rate - all at standard rate
            proofGallonsAtStandardRate = totalProofGallons;
            standardRateTax = totalProofGallons * StandardTaxRate;
        }

        var totalTaxDue = Math.Round(reducedRateTax + standardRateTax, 2, MidpointRounding.AwayFromZero);
        var effectiveTaxRate = totalProofGallons > 0
            ? Math.Round(totalTaxDue / totalProofGallons, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return new ExciseTaxCalculation
        {
            OrderId = orderId,
            CompanyId = order.CompanyId,
            TotalProofGallons = totalProofGallons,
            ProofGallonsAtReducedRate = proofGallonsAtReducedRate,
            ProofGallonsAtStandardRate = proofGallonsAtStandardRate,
            ReducedRateTax = Math.Round(reducedRateTax, 2, MidpointRounding.AwayFromZero),
            StandardRateTax = Math.Round(standardRateTax, 2, MidpointRounding.AwayFromZero),
            TotalTaxDue = totalTaxDue,
            EffectiveTaxRate = effectiveTaxRate,
            IsEligibleForReducedRate = isEligible,
            EligibilityReason = eligibilityReason,
            CalculationDate = DateTime.UtcNow
        };
    }

    public async Task<TtbTaxDetermination> RecordTaxDeterminationAsync(int orderId, ExciseTaxCalculation calculation)
    {
        _logger.LogInformation("Recording tax determination for order {OrderId}", orderId);

        // Check if tax determination already exists for this order
        var existingDetermination = await _dbContext.TtbTaxDeterminations
            .FirstOrDefaultAsync(td => td.OrderId == orderId);

        if (existingDetermination != null)
        {
            _logger.LogInformation("Tax determination already exists for order {OrderId}", orderId);
            return existingDetermination;
        }

        // Create tax determination record
        var taxDetermination = new TtbTaxDetermination
        {
            CompanyId = calculation.CompanyId,
            OrderId = calculation.OrderId,
            ProofGallons = calculation.TotalProofGallons,
            TaxRate = calculation.EffectiveTaxRate,
            TaxAmount = calculation.TotalTaxDue,
            DeterminationDate = DateTime.UtcNow,
            Notes = $"Tax calculation: {calculation.ProofGallonsAtReducedRate:F2} PG @ ${ReducedTaxRate}/PG + " +
                   $"{calculation.ProofGallonsAtStandardRate:F2} PG @ ${StandardTaxRate}/PG. {calculation.EligibilityReason}"
        };

        _dbContext.TtbTaxDeterminations.Add(taxDetermination);

        // Create TTB transaction for tax determination
        var ttbTransaction = new TtbTransaction
        {
            CompanyId = calculation.CompanyId,
            TransactionDate = DateTime.UtcNow,
            TransactionType = TtbTransactionType.TaxDetermination,
            ProductType = "Distilled Spirits",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = calculation.TotalProofGallons,
            WineGallons = 0, // Tax determination is based on proof gallons only
            SourceEntityType = nameof(Order),
            SourceEntityId = orderId,
            Notes = $"Federal excise tax determination: ${calculation.TotalTaxDue:F2} due on {calculation.TotalProofGallons:F2} proof gallons"
        };

        _dbContext.TtbTransactions.Add(ttbTransaction);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created tax determination {TaxDeterminationId} for order {OrderId}: {TaxAmount:C2} on {ProofGallons:F2} PG",
            taxDetermination.Id,
            orderId,
            calculation.TotalTaxDue,
            calculation.TotalProofGallons);

        return taxDetermination;
    }

    public async Task<bool> PostTaxLiabilityToQuickBooksAsync(int taxDeterminationId)
    {
        _logger.LogInformation("Posting tax liability to QuickBooks for tax determination {TaxDeterminationId}", taxDeterminationId);

        var taxDetermination = await _dbContext.TtbTaxDeterminations
            .Include(td => td.Order)
            .FirstOrDefaultAsync(td => td.Id == taxDeterminationId)
            ?? throw new InvalidOperationException($"Tax determination {taxDeterminationId} was not found.");

        // Check if already posted to QuickBooks
        if (!string.IsNullOrEmpty(taxDetermination.QuickBooksJournalEntryId))
        {
            _logger.LogInformation(
                "Tax determination {TaxDeterminationId} already posted to QuickBooks with entry {JournalEntryId}",
                taxDeterminationId,
                taxDetermination.QuickBooksJournalEntryId);
            return true;
        }

        // Check if QuickBooks integration is enabled
        var integration = await _dbContext.AccountingIntegrations
            .FirstOrDefaultAsync(ai => ai.CompanyId == taxDetermination.CompanyId && ai.IsActive);

        if (integration == null)
        {
            _logger.LogInformation("QuickBooks integration not enabled for company {CompanyId}", taxDetermination.CompanyId);
            return false;
        }

        var entityType = "TaxDetermination";
        var entityId = taxDeterminationId.ToString(CultureInfo.InvariantCulture);

        var syncLog = await _syncLogService.GetOrCreateSyncLogAsync(
            taxDetermination.CompanyId,
            entityType,
            entityId);

        try
        {
            _syncLogService.UpdateSyncLog(syncLog, SyncStatus.InProgress, null, syncLog.ExternalEntityId);
            await _dbContext.SaveChangesAsync();

            // Validate required account mappings exist
            await _accountMappingService.ValidateRequiredMappingsExistAsync(
                taxDetermination.CompanyId,
                CaskrAccountType.ExciseTaxExpense,
                CaskrAccountType.ExciseTaxPayable);

            var accountMappings = await _accountMappingService.LoadAccountMappingsAsync(taxDetermination.CompanyId);
            var expenseAccount = accountMappings[CaskrAccountType.ExciseTaxExpense];
            var payableAccount = accountMappings[CaskrAccountType.ExciseTaxPayable];

            // Create QuickBooks service context
            var integrationContext = await _integrationContextFactory.CreateAsync(taxDetermination.CompanyId);
            var serviceContext = integrationContext.ServiceContext;

            // Build journal entry
            var description = $"Federal excise tax - Order {taxDetermination.Order.Name} ({taxDetermination.ProofGallons:F2} PG)";
            var journalEntry = BuildTaxLiabilityJournalEntry(
                taxDetermination.TaxAmount,
                description,
                expenseAccount,
                payableAccount);

            var createdEntry = await _journalEntryClient.CreateJournalEntryAsync(
                serviceContext,
                journalEntry,
                CancellationToken.None);

            // Update tax determination with QuickBooks journal entry ID
            taxDetermination.QuickBooksJournalEntryId = createdEntry.Id;
            _syncLogService.UpdateSyncLog(syncLog, SyncStatus.Success, null, createdEntry.Id);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Posted tax liability for determination {TaxDeterminationId} to QuickBooks with entry {JournalEntryId}",
                taxDeterminationId,
                createdEntry.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post tax liability to QuickBooks for determination {TaxDeterminationId}", taxDeterminationId);
            _syncLogService.IncrementRetryCount(syncLog);
            _syncLogService.UpdateSyncLog(syncLog, SyncStatus.Failed, ex.Message, syncLog.ExternalEntityId);
            await _dbContext.SaveChangesAsync();
            return false;
        }
    }

    public async Task<ExciseTaxReport> GenerateExciseTaxReportAsync(int companyId, int month, int year)
    {
        _logger.LogInformation("Generating excise tax report for company {CompanyId}, {Month}/{Year}", companyId, month, year);

        var company = await _dbContext.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId)
            ?? throw new InvalidOperationException($"Company {companyId} was not found.");

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Get all tax determinations for the month
        var taxDeterminations = await _dbContext.TtbTaxDeterminations
            .Include(td => td.Order)
            .Where(td => td.CompanyId == companyId &&
                        td.DeterminationDate >= startDate &&
                        td.DeterminationDate <= endDate)
            .OrderBy(td => td.DeterminationDate)
            .ToListAsync();

        var totalProofGallons = taxDeterminations.Sum(td => td.ProofGallons);
        var totalTaxDue = taxDeterminations.Sum(td => td.TaxAmount);
        var totalTaxPaid = taxDeterminations.Where(td => td.PaidDate.HasValue).Sum(td => td.TaxAmount);
        var outstandingLiability = totalTaxDue - totalTaxPaid;

        var report = new ExciseTaxReport
        {
            CompanyId = companyId,
            CompanyName = company.CompanyName,
            Month = month,
            Year = year,
            TotalProofGallonsRemoved = totalProofGallons,
            TotalTaxDue = totalTaxDue,
            TotalTaxPaid = totalTaxPaid,
            OutstandingTaxLiability = outstandingLiability,
            Determinations = taxDeterminations.Select(td => new TaxDeterminationSummary
            {
                TaxDeterminationId = td.Id,
                OrderId = td.OrderId,
                OrderName = td.Order.Name,
                DeterminationDate = td.DeterminationDate,
                ProofGallons = td.ProofGallons,
                TaxRate = td.TaxRate,
                TaxAmount = td.TaxAmount,
                IsPaid = td.PaidDate.HasValue,
                PaidDate = td.PaidDate,
                PaymentReference = td.PaymentReference
            }).ToList()
        };

        return report;
    }

    private (bool IsEligible, string Reason) DetermineEligibility(Company company)
    {
        // Check explicit eligibility flag
        if (!company.IsEligibleForReducedExciseTaxRate)
        {
            return (false, company.ExciseTaxEligibilityNotes ?? "Company marked as ineligible for reduced rate");
        }

        // Check annual production limit if set
        if (company.AnnualProductionProofGallons.HasValue &&
            company.AnnualProductionProofGallons.Value >= AnnualProductionEligibilityLimit)
        {
            return (false, $"Annual production ({company.AnnualProductionProofGallons:F0} PG) exceeds limit of {AnnualProductionEligibilityLimit:F0} PG");
        }

        // Eligible for reduced rate under Craft Beverage Modernization Act
        return (true, "Eligible for reduced rate under Craft Beverage Modernization Act");
    }

    private async Task<decimal> GetYearToDateProofGallonsAsync(int companyId, DateTime yearStart)
    {
        var ytdProofGallons = await _dbContext.TtbTaxDeterminations
            .Where(td => td.CompanyId == companyId && td.DeterminationDate >= yearStart)
            .SumAsync(td => td.ProofGallons);

        return ytdProofGallons;
    }

    private static JournalEntry BuildTaxLiabilityJournalEntry(
        decimal taxAmount,
        string description,
        ChartOfAccountsMapping expenseAccount,
        ChartOfAccountsMapping payableAccount)
    {
        var debitLine = CreateJournalLine(description, taxAmount, expenseAccount, PostingTypeEnum.Debit);
        var creditLine = CreateJournalLine(description, taxAmount, payableAccount, PostingTypeEnum.Credit);

        return new JournalEntry
        {
            DocNumber = $"EXCISE-{Guid.NewGuid():N}".ToUpperInvariant(),
            PrivateNote = description,
            TxnDate = DateTime.UtcNow.Date,
            TxnDateSpecified = true,
            Line = new[] { debitLine, creditLine }
        };
    }

    private static Line CreateJournalLine(
        string description,
        decimal amount,
        ChartOfAccountsMapping account,
        PostingTypeEnum postingType)
    {
        return new Line
        {
            Description = description,
            Amount = amount,
            DetailType = LineDetailTypeEnum.JournalEntryLineDetail,
            AnyIntuitObject = new JournalEntryLineDetail
            {
                PostingType = postingType,
                PostingTypeSpecified = true,
                AccountRef = new ReferenceType
                {
                    Value = account.QboAccountId,
                    name = account.QboAccountName
                }
            }
        };
    }
}
