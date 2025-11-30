namespace Caskr.server.Models;

public enum TtbReportStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    ValidationFailed = 4,
    /// <summary>
    /// Report has been submitted for internal review before TTB submission.
    /// </summary>
    PendingReview = 5,
    /// <summary>
    /// Report has been submitted to TTB and archived for record keeping.
    /// </summary>
    Archived = 6
}

public enum TtbFormType
{
    Form5110_28 = 0,
    Form5110_40 = 1
}

public enum TtbAutoReportCadence
{
    Monthly = 0,
    Weekly = 1
}

public enum TtbTransactionType
{
    Production = 0,
    TransferIn = 1,
    TransferOut = 2,
    Loss = 3,
    Gain = 4,
    TaxDetermination = 5,
    Destruction = 6,
    Bottling = 7
}

public enum TtbSpiritsType
{
    Under190Proof = 0,
    Neutral190OrMore = 1,
    Alcohol = 2,
    Wine = 3
}

public enum TtbTaxStatus
{
    Bonded = 0,
    TaxPaid = 1,
    Export = 2,
    TaxFree = 3
}

public enum TtbGaugeType
{
    Fill = 0,
    Storage = 1,
    Removal = 2
}
