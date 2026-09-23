namespace DocImageSort.Api.Models;

public enum DocumentStatus
{
    Pending,
    Classified,
    Converted,
    Filed,
    Duplicate,
    Error
}

/// <summary>
/// A single document processed through the DocImageSort pipeline.
/// </summary>
public class Document : BaseEntity
{
    public int? LoanFileId { get; set; }
    public LoanFile? LoanFile { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string RenamedFileName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string FiledPath { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string AiClassificationNotes { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; }
}
