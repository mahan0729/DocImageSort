namespace DocImageSort.Api.Services.Interfaces;

/// <summary>
/// Orchestrates the full document processing pipeline:
/// Ingest → Duplicate Check → Classify → Convert → Rename → Route.
/// </summary>
public interface IDocumentPipelineService
{
    /// <summary>
    /// Processes a single file through the complete pipeline.
    /// Unsupported file types and duplicates are handled gracefully without throwing.
    /// </summary>
    /// <param name="filePath">Absolute path to the file to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);
}
