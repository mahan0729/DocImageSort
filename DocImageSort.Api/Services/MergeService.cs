using DocImageSort.Api.Services.Interfaces;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DocImageSort.Api.Services;

/// <summary>
/// Merges multiple document pages into a single ordered PDF.
/// AI detects the printed page number on each page for sorting; upload order is the fallback.
/// </summary>
public class MergeService : IMergeService
{
    private readonly IClassificationService _classifier;
    private readonly IConversionService _converter;
    private readonly IAutoCropService _autoCropper;
    private readonly ILogger<MergeService> _logger;

    public MergeService(
        IClassificationService classifier,
        IConversionService converter,
        IAutoCropService autoCropper,
        ILogger<MergeService> logger)
    {
        _classifier  = classifier;
        _converter   = converter;
        _autoCropper = autoCropper;
        _logger      = logger;
    }

    /// <inheritdoc/>
    public async Task<string> MergeFilesAsync(
        IEnumerable<string> filePaths,
        CancellationToken cancellationToken = default)
    {
        var paths = filePaths.ToList();
        if (paths.Count == 0)
            throw new ArgumentException("At least one file is required.", nameof(filePaths));

        try
        {
            // Auto-crop each image before classification (removes desk/background from phone photos)
            var cropTasks = paths.Select(p => _autoCropper.CropToDocumentAsync(p, cancellationToken));
            await Task.WhenAll(cropTasks);

            // Classify page numbers in parallel (best-effort; falls back to upload order)
            var classifyTasks = paths.Select((path, idx) => ClassifyPageAsync(path, idx, cancellationToken));
            var pageInfos = await Task.WhenAll(classifyTasks);

            // Pages with a detected number sort first; ties broken by upload index
            var sorted = pageInfos
                .OrderBy(p => p.PageNumber ?? (p.UploadIndex + 10_000))
                .ThenBy(p => p.UploadIndex)
                .ToList();

            _logger.LogInformation("Merge order ({Count} pages): {Order}",
                sorted.Count,
                string.Join(", ", sorted.Select(p =>
                    $"p{p.PageNumber?.ToString() ?? "?"}={Path.GetFileName(p.OriginalPath)}")));

            // Convert images → PDF (PDFs pass through unchanged).
            // Track in a list so temp files can be cleaned up in finally even if an error occurs mid-loop.
            var pdfPaths = new List<(string Path, bool IsTemp)>();
            string mergedPath;
            try
            {
                foreach (var page in sorted)
                {
                    var pdfPath = await _converter.ConvertToPdfAsync(page.OriginalPath, cancellationToken);
                    pdfPaths.Add((pdfPath, pdfPath != page.OriginalPath));
                }

                // Merge all pages into one PDF
                mergedPath = Path.Combine(Path.GetTempPath(), $"merge_{Guid.NewGuid():N}.pdf");
                using (var outputDoc = new PdfDocument())
                {
                    foreach (var (pdfPath, _) in pdfPaths)
                    {
                        using var inputDoc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Import);
                        for (var i = 0; i < inputDoc.PageCount; i++)
                            outputDoc.AddPage(inputDoc.Pages[i]);
                    }
                    outputDoc.Save(mergedPath);
                }
            }
            finally
            {
                // Always clean up temp conversion files regardless of success or failure.
                foreach (var (pdfPath, isTemp) in pdfPaths)
                {
                    if (isTemp && File.Exists(pdfPath))
                        File.Delete(pdfPath);
                }
            }

            _logger.LogInformation("Merged {Count} pages → {Output}", sorted.Count, Path.GetFileName(mergedPath));
            return mergedPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merge failed for {Count} files", paths.Count);
            throw;
        }
    }

    private async Task<PageInfo> ClassifyPageAsync(string path, int uploadIndex, CancellationToken ct)
    {
        var result = await _classifier.ClassifyPageNumberAsync(path, ct);
        return new PageInfo(path, uploadIndex, result.PageNumber);
    }

    private record PageInfo(string OriginalPath, int UploadIndex, int? PageNumber);
}
