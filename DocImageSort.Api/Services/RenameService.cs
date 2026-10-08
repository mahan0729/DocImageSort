using DocImageSort.Api.Models;
using DocImageSort.Api.Services.Interfaces;

namespace DocImageSort.Api.Services;

/// <summary>
/// Generates and applies standardized file names.
/// Format: [LastName]_[FirstName]_[DocumentType]_[Qualifier]_[MMDDYYYY].pdf
/// Date range format (bank statements): [LastName]_[FirstName]_[DocumentType]_[Qualifier]_[MMDDYYYYtoMMDDYYYY].pdf
/// Pre-assignment: PENDING_[DocumentType]_[Qualifier]_[MMDDYYYY].pdf
/// Only A–Z, a–z, 0–9, and _ are allowed; spaces become underscores.
/// W2 encodes tax year in the type segment: W2_YYYY (e.g. W2_2024).
/// 1099 types encode tax year in the type segment: 1099_Composite_2024.
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
        var namePart = borrower is not null
            ? $"{Sanitize(borrower.LastName)}_{Sanitize(borrower.FirstName)}"
            : "PENDING";

        var docType = Sanitize(document.DocumentType ?? "Unknown");

        // W2: embed tax year inside the type segment → W2_2024
        if (document.DocumentType == "W2")
        {
            var taxYear = document.DocumentDate.HasValue
                ? document.DocumentDate.Value.ToString("yyyy")
                : DateTime.UtcNow.ToString("yyyy");
            docType = $"W2_{taxYear}";
        }

        // 1099 types: embed tax year → 1099_Composite_2024
        if (document.DocumentType is not null && document.DocumentType.StartsWith("1099"))
        {
            var taxYear = document.DocumentDate.HasValue
                ? document.DocumentDate.Value.ToString("yyyy")
                : DateTime.UtcNow.ToString("yyyy");
            docType = $"{Sanitize(document.DocumentType)}_{taxYear}";
        }

        var qualifier = string.IsNullOrWhiteSpace(document.DocumentQualifier)
            ? null
            : Sanitize(document.DocumentQualifier);

        // Date: use document date if available, otherwise today. Full MMDDYYYY format.
        var datePart = BuildDatePart(document);

        var typePart = qualifier is not null ? $"{docType}_{qualifier}" : docType;

        return $"{namePart}_{typePart}_{datePart}.pdf";
    }

    private static string BuildDatePart(Document document)
    {
        var start = document.DocumentDate ?? DateTime.UtcNow;
        var startStr = start.ToString("MMddyyyy");

        if (document.DocumentEndDate.HasValue && document.DocumentEndDate.Value != document.DocumentDate)
        {
            var endStr = document.DocumentEndDate.Value.ToString("MMddyyyy");
            return $"{startStr}to{endStr}";
        }

        return startStr;
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
                    if (counter > 999) throw new InvalidOperationException($"Too many files with the same name in {directory}.");
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
