namespace DocImageSort.Api.Services.Interfaces;

/// <summary>
/// The result of an AI document classification attempt.
/// </summary>
/// <param name="DocumentType">The identified document type (e.g. "Pay Stub", "W2").</param>
/// <param name="DocumentDate">The document date in YYYY-MM-DD, YYYY-MM, or YYYY format, or null if not found.</param>
/// <param name="Notes">Brief description of what the AI observed (max 100 chars).</param>
/// <param name="Success">False if classification was skipped or failed; DocumentType will be "Unknown".</param>
/// <param name="SubjectName">For W2: the employee name printed on the form. Null for other doc types.</param>
/// <param name="AccountType">For Bank Statement: account type (e.g. "Checking", "Savings"). Null otherwise.</param>
/// <param name="InstitutionName">For Bank Statement: the bank or institution name (e.g. "Chase"). Null otherwise.</param>
public record ClassificationResult(
    string  DocumentType,
    string? DocumentDate,
    string  Notes,
    bool    Success,
    string? SubjectName     = null,
    string? AccountType     = null,
    string? InstitutionName = null,
    string? DocumentEndDate = null
);

/// <summary>
/// The result of extracting a page number from a single page of a multi-page document.
/// Used by the merge feature to sort pages into the correct order.
/// </summary>
/// <param name="PageNumber">The page number detected (e.g. 2 from "Page 2 of 4"), or null if not visible.</param>
/// <param name="TotalPages">Total number of pages shown on the document (e.g. 4), or null if not shown.</param>
/// <param name="Notes">Brief note about what was observed on the page.</param>
/// <param name="Success">False if the API key is missing or the call failed.</param>
public record MergePageResult(
    int?   PageNumber,
    int?   TotalPages,
    string Notes,
    bool   Success
);

/// <summary>
/// Sends a document to Claude AI and returns its identified type and date.
/// Supports PDF, JPG, and PNG input files.
/// </summary>
public interface IClassificationService
{
    /// <summary>
    /// Classifies the document at the given path using the Claude AI API.
    /// Returns a result with <see cref="ClassificationResult.Success"/> = false if the API key
    /// is not configured or if the API call fails; never throws.
    /// </summary>
    /// <param name="filePath">Absolute path to the PDF, JPG, or PNG file to classify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ClassificationResult> ClassifyAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the page number from a single page of a multi-page document.
    /// Used by <see cref="IMergeService"/> to sort pages into the correct order before merging.
    /// Returns a result with <see cref="MergePageResult.Success"/> = false if the API key
    /// is not configured or the call fails; never throws.
    /// </summary>
    /// <param name="filePath">Absolute path to the PDF, JPG, or PNG file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<MergePageResult> ClassifyPageNumberAsync(string filePath, CancellationToken cancellationToken = default);
}
