namespace DocImageSort.Api.Services.Interfaces;

public interface IConversionService
{
    /// <summary>
    /// Converts a JPG or PNG image file to PDF. Returns the output PDF path.
    /// If the file is already a PDF, returns the original path unchanged.
    /// </summary>
    Task<string> ConvertToPdfAsync(string filePath, CancellationToken cancellationToken = default);
}
