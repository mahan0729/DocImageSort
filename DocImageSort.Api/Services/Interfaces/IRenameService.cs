using DocImageSort.Api.Models;

namespace DocImageSort.Api.Services.Interfaces;

/// <summary>
/// Generates and applies standardized file names for processed documents.
/// </summary>
public interface IRenameService
{
    /// <summary>
    /// Generates the standardized file name for a document without moving any files.
    /// </summary>
    /// <param name="document">The document whose type and date are used for naming.</param>
    /// <param name="borrower">
    /// The assigned borrower. When provided, the name is <c>LastName,FirstName_DocType_YYYY-MM.pdf</c>.
    /// When null, the name is <c>PENDING_DocType_YYYY-MM.pdf</c>.
    /// </param>
    /// <returns>The generated file name (not a full path).</returns>
    string GenerateFileName(Document document, Borrower? borrower = null);

    /// <summary>
    /// Renames the physical file on disk to the standardized name, within its current directory.
    /// Appends a numeric counter suffix if the target name already exists.
    /// </summary>
    /// <param name="document">The document to rename; <c>SourcePath</c> must point to the current file.</param>
    /// <param name="borrower">The assigned borrower, or null to use the PENDING prefix.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The absolute path of the renamed file.</returns>
    Task<string> RenameFileAsync(Document document, Borrower? borrower = null, CancellationToken cancellationToken = default);
}
