using DocImageSort.Api.Data;
using DocImageSort.Api.Models;
using DocImageSort.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocImageSort.Api.Controllers;

/// <summary>
/// Read + review endpoints for processed documents.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDocumentReviewService _review;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(AppDbContext db, IDocumentReviewService review, ILogger<DocumentsController> logger)
    {
        _db = db;
        _review = review;
        _logger = logger;
    }

    /// <summary>
    /// Lists documents, optionally filtered by status or file name search.
    /// GET /api/documents?status=Pending&amp;search=smith
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var query = _db.Documents
            .Include(d => d.LoanFile)
                .ThenInclude(lf => lf!.Borrower)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<DocumentStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(d => d.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(d =>
                d.OriginalFileName.ToLower().Contains(term) ||
                d.DocumentType.ToLower().Contains(term));
        }

        var docs = await query
            .OrderByDescending(d => d.CreatedDate)
            .Select(d => new DocumentDto(d))
            .ToListAsync(ct);

        return Ok(docs);
    }

    /// <summary>GET /api/documents/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var doc = await _db.Documents
            .Include(d => d.LoanFile)
                .ThenInclude(lf => lf!.Borrower)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (doc is null) return NotFound();
        return Ok(new DocumentDto(doc));
    }

    /// <summary>
    /// Correct the AI-assigned document type.
    /// PUT /api/documents/{id}/correct-type
    /// </summary>
    [HttpPut("{id:int}/correct-type")]
    public async Task<IActionResult> CorrectType(int id, [FromBody] CorrectTypeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.DocumentType))
            return BadRequest("DocumentType is required.");

        try
        {
            await _review.CorrectTypeAsync(id, req.DocumentType, ct);
            var doc = await _db.Documents.FindAsync([id], ct);
            return Ok(new DocumentDto(doc!));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CorrectType failed for document {Id}", id);
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Assign a borrower — triggers final rename and route out of PENDING.
    /// POST /api/documents/{id}/assign
    /// </summary>
    [HttpPost("{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignRequest req, CancellationToken ct)
    {
        try
        {
            await _review.AssignBorrowerAsync(id, req.BorrowerId, ct);
            var doc = await _db.Documents
                .Include(d => d.LoanFile)
                    .ThenInclude(lf => lf!.Borrower)
                .FirstOrDefaultAsync(d => d.Id == id, ct);
            return Ok(new DocumentDto(doc!));
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assign failed for document {Id}", id);
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>Returns the list of known mortgage document types for UI dropdowns.</summary>
    [HttpGet("types")]
    public IActionResult GetDocumentTypes() => Ok(KnownDocumentTypes.All);
}

// ── DTOs / requests ───────────────────────────────────────────────────────────

public record CorrectTypeRequest(string DocumentType);
public record AssignRequest(int BorrowerId);

public record DocumentDto(
    int     Id,
    string  OriginalFileName,
    string  RenamedFileName,
    string  DocumentType,
    string  Status,
    string? AiClassificationNotes,
    string? DocumentDate,
    string  SourcePath,
    string  FiledPath,
    int?    BorrowerId,
    string? BorrowerName,
    string? LoanNumber,
    DateTime CreatedDate
)
{
    public DocumentDto(Document d) : this(
        d.Id,
        d.OriginalFileName,
        d.RenamedFileName,
        d.DocumentType,
        d.Status.ToString(),
        d.AiClassificationNotes,
        d.DocumentDate?.ToString("yyyy-MM-dd"),
        d.SourcePath,
        d.FiledPath,
        d.LoanFile?.BorrowerId,
        d.LoanFile?.Borrower?.FolderName,
        d.LoanFile?.LoanNumber,
        d.CreatedDate
    ) { }
}

public static class KnownDocumentTypes
{
    public static readonly string[] All =
    [
        "Pay Stub",
        "Bank Statement",
        "W-2",
        "1040 Tax Return",
        "1099",
        "Driver's License",
        "Social Security Card",
        "Mortgage Statement",
        "Homeowners Insurance",
        "Purchase Agreement",
        "Appraisal Report",
        "Title Report",
        "HOA Statement",
        "Gift Letter",
        "Verification of Employment",
        "Unknown",
    ];
}
