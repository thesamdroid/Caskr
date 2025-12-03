namespace Caskr.server.Models.Portal;

/// <summary>
/// Documents uploaded by distillery staff for customer access
/// </summary>
public class PortalDocument
{
    public long Id { get; set; }

    public long CaskOwnershipId { get; set; }

    public PortalDocumentType DocumentType { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public long? FileSizeBytes { get; set; }

    public string? MimeType { get; set; }

    /// <summary>
    /// Distillery staff member who uploaded this document
    /// </summary>
    public int UploadedByUserId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual CaskOwnership CaskOwnership { get; set; } = null!;

    public virtual User UploadedByUser { get; set; } = null!;
}

public enum PortalDocumentType
{
    Ownership_Certificate,
    Insurance_Document,
    Maturation_Report,
    Photo,
    Invoice,
    Other
}
