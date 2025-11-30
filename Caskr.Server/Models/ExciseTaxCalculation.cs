namespace Caskr.server.Models;

/// <summary>
/// Result of federal excise tax calculation
/// </summary>
public class ExciseTaxCalculation
{
    public int OrderId { get; set; }
    public int CompanyId { get; set; }
    public decimal TotalProofGallons { get; set; }
    public decimal ProofGallonsAtReducedRate { get; set; }
    public decimal ProofGallonsAtStandardRate { get; set; }
    public decimal ReducedRateTax { get; set; }
    public decimal StandardRateTax { get; set; }
    public decimal TotalTaxDue { get; set; }
    public decimal EffectiveTaxRate { get; set; }
    public bool IsEligibleForReducedRate { get; set; }
    public string EligibilityReason { get; set; } = string.Empty;
    public DateTime CalculationDate { get; set; }
}

/// <summary>
/// Request to calculate excise tax for an order
/// </summary>
public class ExciseTaxCalculationRequest
{
    public int OrderId { get; set; }
    public int CompanyId { get; set; }
}

/// <summary>
/// Monthly excise tax report
/// </summary>
public class ExciseTaxReport
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalProofGallonsRemoved { get; set; }
    public decimal TotalTaxDue { get; set; }
    public decimal TotalTaxPaid { get; set; }
    public decimal OutstandingTaxLiability { get; set; }
    public List<TaxDeterminationSummary> Determinations { get; set; } = new();
}

/// <summary>
/// Summary of individual tax determination
/// </summary>
public class TaxDeterminationSummary
{
    public int TaxDeterminationId { get; set; }
    public int OrderId { get; set; }
    public string OrderName { get; set; } = string.Empty;
    public DateTime DeterminationDate { get; set; }
    public decimal ProofGallons { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? PaymentReference { get; set; }
}
