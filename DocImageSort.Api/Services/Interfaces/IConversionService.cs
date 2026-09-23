namespace DocImageSort.Api.Services.Interfaces;

/// <summary>
/// Converts image files to PDF for uniform downstream processing.
/// </summary>
public interface IConversionService
{
    /// <summary>
    /// Converts a JPG or PNG image file to a single-page PDF saved alongside the source file.
    /// If the input is already a PDF, returns <paramref name="filePath"/> unchanged.
    /// </summary>
    /// <param name="filePath">Absolute path to the source file (PDF, JPG, or PNG).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The absolute path of the output PDF. Equal to <paramref name="filePath"/> when no conversion was needed.
    /// </returns>
    Task<string> ConvertToPdfAsync(string filePath, CancellationToken cancellationToken = default);
}
