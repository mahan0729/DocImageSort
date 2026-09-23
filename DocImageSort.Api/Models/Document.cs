namespace DocImageSort.Api.Models;

/// <summary>Tracks the current stage of a document in the pipeline.</summary>
public enum DocumentStatus
{
    /// <summary>File received and record created; pipeline not yet started.</summary>
    Pending,
    /// <summary>AI classification completed successfully.</summary>
    Classified,
    /// <summary>Image converted to PDF.</summary>
    Converted,
    /// <summary>Renamed and routed to the borrower's folder.</summary>
    Filed,
    /// <summary>File hash matched an existing document — pipeline skipped.</summary>
    Duplicate,
    /// <summary>An error occurred during pipeline processing.</summary>
    Error
}

/// <summary>
/// A single document processed through the DocImageSort pipeline.
/// Documents without a borrower assignment land in the PENDING folder
/// until assigned from the Document Review screen.
/// </summary>
public class Document : BaseEntity
{
    /// <summary>Foreign key to the loan file this document belongs to. Null until a borrower is assigned.</summary>
    public int? LoanFileId { get; set; }

    /// <summary>Navigation property to the associated loan file.</summary>
    public LoanFile? LoanFile { get; set; }

    /// <summary>Original file name as it arrived in the drop folder.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Standardized file name applied by the rename step (e.g. <c>Smith,John_Pay Stub_2026-08.pdf</c>).</summary>
    public string RenamedFileName { get; set; } = string.Empty;

    /// <summary>Document type identified by AI classification (e.g. "Pay Stub", "W-2").</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Current absolute path of the file on disk (updated at each pipeline step).</summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>Final absolute path after the document was routed to its borrower folder.</summary>
    public string FiledPath { get; set; } = string.Empty;

    /// <summary>File extension of the original drop file (e.g. ".pdf", ".jpg").</summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>Current pipeline status.</summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    /// <summary>AI-generated description of the document content (max 100 chars).</summary>
    public string AiClassificationNotes { get; set; } = string.Empty;

    /// <summary>Date on the document as extracted by the AI, in YYYY-MM or YYYY format.</summary>
    public DateTime? DocumentDate { get; set; }

    /// <summary>SHA-256 hex hash of the file contents, used for duplicate detection.</summary>
    public string FileHash { get; set; } = string.Empty;
}
