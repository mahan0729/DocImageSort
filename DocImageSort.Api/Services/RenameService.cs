using DocImageSort.Api.Models;
using DocImageSort.Api.Services.Interfaces;

namespace DocImageSort.Api.Services;

/// <summary>
/// Generates and applies standardized file names.
/// Format: [LoanNumber]_[DocumentType]_[MMDDYY].pdf  (Chance Nelson convention)
/// Pre-assignment: PENDING_[DocumentType]_[MMDDYY].pdf
/// Only A–Z, a–z, 0–9, and _ are allowed; spaces become underscores.
/// W2 encodes tax year in the type segment: W2_YY (e.g. W2_24).
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
        var loanPart = borrower is not null
            ? Sanitize(borrower.LoanNumber)
            : "PENDING";

        var docType = Sanitize(document.DocumentType ?? "Unknown");

        // W2: embed tax year inside the type segment → W2_24
        if (document.DocumentType == "W2")
        {
            var taxYear = document.DocumentDate.HasValue
                ? document.DocumentDate.Value.ToString("yy")
                : DateTime.UtcNow.ToString("yy");
            docType = $"W2_{taxYear}";
        }

        var qualifier = string.IsNullOrWhiteSpace(document.DocumentQualifier)
            ? null
            : Sanitize(document.DocumentQualifier);

        var date = DateTime.UtcNow.ToString("MMddyy");

        var typePart = qualifier is not null ? $"{docType}_{qualifier}" : docType;

        return $"{loanPart}_{typePart}_{date}.pdf";
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

    /// <summary>Enforces [A-Za-z0-9_] per Chance's naming convention. Spaces become underscores; all other non-conforming chars are stripped.</summary>
    private static string Sanitize(string input) =>
        string.Concat(input.Select(c => c == ' ' ? '_' : char.IsLetterOrDigit(c) || c == '_' ? c : '\0'))
              .Replace("\0", string.Empty)
              .Trim('_');
}
