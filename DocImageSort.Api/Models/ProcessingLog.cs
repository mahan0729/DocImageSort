namespace DocImageSort.Api.Models;

public enum LogLevel
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Tracks every document ingested, action taken, and outcome.
/// </summary>
public class ProcessingLog : BaseEntity
{
    public int? DocumentId { get; set; }
    public Document? Document { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public LogLevel Level { get; set; } = LogLevel.Info;
}
