using DocImageSort.Api.Data;
using DocImageSort.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocImageSort.Api.Controllers;

/// <summary>
/// CRUD endpoints for borrower management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BorrowersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<BorrowersController> _logger;

    public BorrowersController(AppDbContext db, ILogger<BorrowersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns all borrowers, optionally filtered by name or loan number.
    /// GET /api/borrowers?search=smith
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct)
    {
        try
        {
            var query = _db.Borrowers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(b =>
                    b.LastName.ToLower().Contains(term) ||
                    b.FirstName.ToLower().Contains(term) ||
                    b.LoanNumber.ToLower().Contains(term));
            }

            var borrowers = await query
                .OrderBy(b => b.LastName)
                .ThenBy(b => b.FirstName)
                .Select(b => new BorrowerDto(b))
                .ToListAsync(ct);

            return Ok(borrowers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAll borrowers failed");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>GET /api/borrowers/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        try
        {
            var borrower = await _db.Borrowers.FindAsync([id], ct);
            if (borrower is null) return NotFound();
            return Ok(new BorrowerDto(borrower));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetById borrower {Id} failed", id);
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>POST /api/borrowers</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BorrowerRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var borrower = new Borrower
            {
                LastName          = req.LastName.Trim(),
                FirstName         = req.FirstName.Trim(),
                LoanNumber        = req.LoanNumber.Trim(),
                IsPrimaryBorrower = req.IsPrimaryBorrower,
                CreatedBy         = "system",
                UpdatedBy         = "system"
            };

            _db.Borrowers.Add(borrower);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Borrower created: {Id} — {Name}", borrower.Id, borrower.FolderName);
            return CreatedAtAction(nameof(GetById), new { id = borrower.Id }, new BorrowerDto(borrower));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create borrower failed for {LastName}, {FirstName}", req.LastName, req.FirstName);
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>PUT /api/borrowers/{id}</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] BorrowerRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var borrower = await _db.Borrowers.FindAsync([id], ct);
            if (borrower is null) return NotFound();

            borrower.LastName          = req.LastName.Trim();
            borrower.FirstName         = req.FirstName.Trim();
            borrower.LoanNumber        = req.LoanNumber.Trim();
            borrower.IsPrimaryBorrower = req.IsPrimaryBorrower;
            borrower.UpdatedBy         = "system";
            borrower.UpdatedDate       = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Borrower updated: {Id} — {Name}", borrower.Id, borrower.FolderName);
            return Ok(new BorrowerDto(borrower));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update borrower {Id} failed", id);
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>DELETE /api/borrowers/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var borrower = await _db.Borrowers.FindAsync([id], ct);
            if (borrower is null) return NotFound();

            _db.Borrowers.Remove(borrower);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Borrower deleted: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete borrower {Id} failed", id);
            return StatusCode(500, ex.Message);
        }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record BorrowerRequest(
    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.StringLength(100)]
    string LastName,

    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.StringLength(100)]
    string FirstName,

    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.StringLength(50)]
    string LoanNumber,

    bool IsPrimaryBorrower = true
);

public record BorrowerDto(
    int    Id,
    string LastName,
    string FirstName,
    string LoanNumber,
    bool   IsPrimaryBorrower,
    string FolderName,
    DateTime CreatedDate,
    DateTime UpdatedDate
)
{
    public BorrowerDto(Borrower b) : this(
        b.Id, b.LastName, b.FirstName, b.LoanNumber,
        b.IsPrimaryBorrower, b.FolderName,
        b.CreatedDate, b.UpdatedDate) { }
}
