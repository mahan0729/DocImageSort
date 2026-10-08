namespace DocImageSort.Api.Services.Interfaces;

/// <summary>
/// Merges multiple document pages (PDF/JPG/PNG) into a single ordered PDF.
/// Page order is determined by AI-detected page numbers; upload order is the fallback.
/// </summary>
public interface IMergeService
{
    /// <summary>
    /// Converts and merges the provided files into a single PDF, sorted by detected page number.
    /// Returns the absolute path to the merged temp PDF.
    /// </summary>
    /// <param name="filePaths">Absolute paths to the source files (PDF, JPG, or PNG).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> MergeFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
}
