namespace DocImageSort.Api.Services.Interfaces;

public interface IDocumentReviewService
{
    /// <summary>Overwrites the AI-assigned document type and re-generates the file name.</summary>
    Task CorrectTypeAsync(int documentId, string documentType, CancellationToken ct = default);

    /// <summary>
    /// Links the document to a borrower, renames from PENDING_ prefix, and routes to the borrower folder.
    /// </summary>
    Task AssignBorrowerAsync(int documentId, int borrowerId, CancellationToken ct = default);
}
