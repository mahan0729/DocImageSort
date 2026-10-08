using DocImageSort.Api.Data;
using DocImageSort.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocImageSort.Api.Controllers;

/// <summary>
/// Accepts multiple document pages, merges them into a single ordered PDF,
/// and runs the merged file through the standard classification pipeline.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MergeController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };

    private readonly IMergeService _mergeService;
    private readonly IDocumentPipelineService _pipeline;
    private readonly AppDbContext _db;
    private readonly ILogger<MergeController> _logger;

    public MergeController(
        IMergeService mergeService,
        IDocumentPipelineService pipeline,
        AppDbContext db,
        ILogger<MergeController> logger)
    {
        _mergeService = mergeService;
        _pipeline     = pipeline;
        _db           = db;
        _logger       = logger;
    }

    /// <summary>
    /// POST /api/merge
    /// Accepts 2–20 pages (PDF/JPG/PNG), merges them in AI-detected page-number order,
    /// classifies the merged document, and routes it to PENDING.
    /// Returns the created <see cref="DocumentDto"/>.
    /// </summary>
    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Merge([FromForm] IFormFileCollection files, CancellationToken ct)
    {
        if (files.Count < 2)
            return BadRequest("At least 2 files are required for a merge.");
        if (files.Count > 20)
            return BadRequest("A maximum of 20 files can be merged at once.");

        foreach (var file in files)
        {
            if (file.Length == 0)
                return BadRequest($"File '{file.FileName}' is empty.");
            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext))
                return BadRequest($"Unsupported file type '{file.FileName}'. Accepted: PDF, JPG, PNG.");
        }

        var tempFiles = new List<string>();
        string? mergedPath = null;

        try
        {
            // Save uploads to temp directory
            foreach (var file in files)
            {
                var tempPath = Path.Combine(
                    Path.GetTempPath(),
                    $"upload_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}");

                await using var stream = System.IO.File.Create(tempPath);
                await file.CopyToAsync(stream, ct);
                tempFiles.Add(tempPath);
            }

            // Merge pages into a single ordered PDF
            mergedPath = await _mergeService.MergeFilesAsync(tempFiles, ct);

            // Run the merged PDF through the standard pipeline (classify → rename → route)
            var mergedFileName = Path.GetFileName(mergedPath);
            await _pipeline.ProcessFileAsync(mergedPath, ct);

            // Retrieve the document record created by the pipeline
            var doc = await _db.Documents
                .Include(d => d.LoanFile)
                    .ThenInclude(lf => lf!.Borrower)
                .Where(d => d.OriginalFileName == mergedFileName)
                .OrderByDescending(d => d.CreatedDate)
                .FirstOrDefaultAsync(ct);

            if (doc is null)
                return StatusCode(500, "Document was processed but could not be retrieved from the database.");

            _logger.LogInformation("Merge complete — Document {Id}: {Type}", doc.Id, doc.DocumentType);
            return Ok(new DocumentDto(doc));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merge upload failed");
            return StatusCode(500, ex.Message);
        }
        finally
        {
            // Clean up uploaded temp files
            foreach (var path in tempFiles)
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            // The merged PDF was moved by the pipeline; delete only if it still exists (pipeline error)
            if (mergedPath is not null && System.IO.File.Exists(mergedPath))
                System.IO.File.Delete(mergedPath);
        }
    }
}
