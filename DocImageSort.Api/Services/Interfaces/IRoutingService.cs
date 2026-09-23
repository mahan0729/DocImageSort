using DocImageSort.Api.Models;

namespace DocImageSort.Api.Services.Interfaces;

public interface IRoutingService
{
    /// <summary>
    /// Routes the document's file to the correct folder.
    /// With borrower: [FilesFolder]\LastName,FirstName\
    /// Without borrower: [FilesFolder]\PENDING\
    /// Creates the destination folder if it does not exist.
    /// Returns the final filed path.
    /// </summary>
    Task<string> RouteAsync(Document document, Borrower? borrower = null, CancellationToken cancellationToken = default);
}
