using System.ComponentModel.DataAnnotations.Schema;

namespace DocImageSort.Api.Models;

/// <summary>Severity level of a processing log entry.</summary>
public enum LogLevel
{
    /// <summary>Normal pipeline activity.</summary>
    Info,
    /// <summary>Non-fatal issue (e.g. duplicate detected, unsupported file type).</summary>
    Warning,
    /// <summary>Pipeline step failed; see <see cref="ProcessingLog.Message"/> for details.</summary>
    Error
}

/// <summary>
/// Immutable audit record written at each step of the document pipeline.
/// Every ingest, classification, conversion, rename, route, and error produces one entry.
/// </summary>
public class ProcessingLog : BaseEntity
{
    /// <summary>Foreign key to the associated document. Null for errors that occurred before the document record was created.</summary>
    [Column(Order = 1)]
    public int? DocumentId { get; set; }

    /// <summary>Navigation property to the associated document.</summary>
    public Document? Document { get; set; }

    /// <summary>File name (not full path) of the document being processed when this entry was written.</summary>
    [Column(Order = 2)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>Pipeline step that produced this entry (e.g. "Ingest", "Classify", "Convert", "Rename", "Route").</summary>
    [Column(Order = 3)]
    public string Action { get; set; } = string.Empty;

    /// <summary>Result of the action (e.g. "Success", "Error", "Duplicate").</summary>
    [Column(Order = 4)]
    public string Outcome { get; set; } = string.Empty;

    /// <summary>Human-readable detail message for the action outcome.</summary>
    [Column(Order = 5)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Severity of this log entry.</summary>
    [Column(Order = 6)]
    public LogLevel Level { get; set; } = LogLevel.Info;
}
