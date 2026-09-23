using DocImageSort.Api.Models;

namespace DocImageSort.Api.Services.Interfaces;

public interface IRenameService
{
    /// <summary>
    /// Generates a standardized file name.
    /// With borrower: LastName,FirstName_DocType_Date.pdf
    /// Without borrower: PENDING_DocType_Date.pdf
    /// </summary>
    string GenerateFileName(Document document, Borrower? borrower = null);

    /// <summary>
    /// Renames the physical file on disk to the standardized name.
    /// Returns the new full file path.
    /// </summary>
    Task<string> RenameFileAsync(Document document, Borrower? borrower = null, CancellationToken cancellationToken = default);
}
