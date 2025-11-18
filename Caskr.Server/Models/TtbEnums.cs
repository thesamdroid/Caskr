namespace Caskr.server.Models;

public enum TtbReportStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3
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
