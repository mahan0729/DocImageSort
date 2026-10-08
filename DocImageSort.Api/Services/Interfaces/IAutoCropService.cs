namespace DocImageSort.Api.Services.Interfaces;

/// <summary>
/// Crops a phone-camera image to the boundaries of the document within it.
/// Applies only to JPG and PNG files; PDFs are returned unchanged.
/// When confidence is low or the photo is already well-framed the original file is returned unchanged.
/// </summary>
public interface IAutoCropService
{
    /// <summary>
    /// Attempts to detect and crop to the document boundaries in the image at <paramref name="filePath"/>.
    /// Overwrites the file in place when a meaningful crop is found; otherwise leaves it unchanged.
    /// Never throws — returns the original path on any failure.
    /// </summary>
    /// <param name="filePath">Absolute path to a JPG or PNG file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The same <paramref name="filePath"/> (whether cropped or unchanged).</returns>
    Task<string> CropToDocumentAsync(string filePath, CancellationToken cancellationToken = default);
}
