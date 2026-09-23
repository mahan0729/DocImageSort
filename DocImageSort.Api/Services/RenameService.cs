using DocImageSort.Api.Models;
using DocImageSort.Api.Services.Interfaces;

namespace DocImageSort.Api.Services;

/// <summary>
/// Generates and applies standardized file names.
/// Format: LastName,FirstName_DocType_Date.pdf
/// Pre-assignment: PENDING_DocType_Date.pdf
/// </summary>
public class RenameService : IRenameService
{
    private readonly ILogger<RenameService> _logger;

    public RenameService(ILogger<RenameService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string GenerateFileName(Document document, Borrower? borrower = null)
    {
        var borrowerPart = borrower is not null
            ? Sanitize($"{borrower.LastName},{borrower.FirstName}")
            : "PENDING";

        var docType = Sanitize(document.DocumentType ?? "Unknown");

        var datePart = document.DocumentDate.HasValue
            ? document.DocumentDate.Value.ToString("yyyy-MM")
            : DateTime.UtcNow.ToString("yyyy-MM");

        return $"{borrowerPart}_{docType}_{datePart}.pdf";
    }

    /// <inheritdoc/>
    public Task<string> RenameFileAsync(Document document, Borrower? borrower = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(document.SourcePath) ?? string.Empty;
            var newName = GenerateFileName(document, borrower);
            var newPath = Path.Combine(directory, newName);

            // Avoid overwriting an existing file by appending a counter suffix.
            if (File.Exists(newPath) && newPath != document.SourcePath)
            {
                var counter = 1;
                var nameNoExt = Path.GetFileNameWithoutExtension(newName);
                while (File.Exists(newPath))
                {
                    newPath = Path.Combine(directory, $"{nameNoExt}_{counter++}.pdf");
                }
            }

            if (document.SourcePath != newPath && File.Exists(document.SourcePath))
            {
                File.Move(document.SourcePath, newPath);
                _logger.LogInformation("Renamed {Old} → {New}", Path.GetFileName(document.SourcePath), Path.GetFileName(newPath));
            }

            return Task.FromResult(newPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rename failed for document {Id}", document.Id);
            throw;
        }
    }

    /// <summary>Strips characters that are invalid in file names.</summary>
    private static string Sanitize(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(input.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
    }
}
