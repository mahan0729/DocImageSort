using DocImageSort.Api.Services.Interfaces;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace DocImageSort.Api.Services;

/// <summary>
/// Converts JPG/PNG image files to PDF using PDFsharp (MIT license).
/// PDFs pass through unchanged.
/// </summary>
public class ConversionService : IConversionService
{
    private readonly ILogger<ConversionService> _logger;

    public ConversionService(ILogger<ConversionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<string> ConvertToPdfAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".pdf")
            return Task.FromResult(filePath);

        try
        {
            var outputPath = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetFileName(filePath), ".pdf"));

            using var image = XImage.FromFile(filePath);
            using var document = new PdfDocument();

            var page = document.AddPage();
            page.Width = XUnit.FromPoint(image.PointWidth);
            page.Height = XUnit.FromPoint(image.PointHeight);

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);

            document.Save(outputPath);

            _logger.LogInformation("Converted {Source} → {Output}", Path.GetFileName(filePath), Path.GetFileName(outputPath));

            return Task.FromResult(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF conversion failed for: {File}", filePath);
            throw;
        }
    }
}
