using DocImageSort.Api.Data;
using DocImageSort.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocImageSort.Api.Controllers;

/// <summary>
/// Read-only endpoint for the processing audit log.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProcessingLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProcessingLogsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns log entries, newest first.
    /// GET /api/processinglogs?level=Error&amp;search=smith&amp;limit=200
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? level,
        [FromQuery] string? search,
        [FromQuery] int limit = 500,
        CancellationToken ct = default)
    {
        var query = _db.ProcessingLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(level) &&
            Enum.TryParse<Models.LogLevel>(level, ignoreCase: true, out var parsed))
        {
            query = query.Where(l => l.Level == parsed);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(l =>
                l.FileName.ToLower().Contains(term) ||
                l.Action.ToLower().Contains(term) ||
                l.Message.ToLower().Contains(term));
        }

        var logs = await query
            .OrderByDescending(l => l.CreatedDate)
            .Take(limit)
            .Select(l => new ProcessingLogDto(
                l.Id,
                l.DocumentId,
                l.FileName,
                l.Action,
                l.Outcome,
                l.Message,
                l.Level.ToString(),
                l.CreatedDate))
            .ToListAsync(ct);

        return Ok(logs);
    }
}

public record ProcessingLogDto(
    int      Id,
    int?     DocumentId,
    string   FileName,
    string   Action,
    string   Outcome,
    string   Message,
    string   Level,
    DateTime CreatedDate
);
