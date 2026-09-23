using DocImageSort.Api.Data;
using DocImageSort.Api.Models;
using DocImageSort.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using AppLogLevel = DocImageSort.Api.Models.LogLevel;

namespace DocImageSort.Api.Services;

/// <summary>
/// Handles human review actions: correcting AI classification and assigning a borrower.
/// Assigning triggers the final rename (PENDING_ → LastName,FirstName_) and route to borrower folder.
/// </summary>
public class DocumentReviewService : IDocumentReviewService
{
    private readonly AppDbContext _db;
    private readonly IRenameService _renamer;
    private readonly IRoutingService _router;
    private readonly ILogger<DocumentReviewService> _logger;

    public DocumentReviewService(
        AppDbContext db,
        IRenameService renamer,
        IRoutingService router,
        ILogger<DocumentReviewService> logger)
    {
        _db = db;
        _renamer = renamer;
        _router = router;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task CorrectTypeAsync(int documentId, string documentType, CancellationToken ct = default)
    {
        var document = await _db.Documents.FindAsync([documentId], ct)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        var oldType = document.DocumentType;
        document.DocumentType = documentType.Trim();
        document.UpdatedBy = "system";
        document.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await LogAsync(document.Id, document.RenamedFileName, "CorrectType", "Success",
            $"Type changed from '{oldType}' to '{document.DocumentType}'", AppLogLevel.Info, ct);

        _logger.LogInformation("Document {Id} type corrected: {Old} → {New}", documentId, oldType, documentType);
    }

    /// <inheritdoc/>
    public async Task AssignBorrowerAsync(int documentId, int borrowerId, CancellationToken ct = default)
    {
        var document = await _db.Documents.FindAsync([documentId], ct)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        var borrower = await _db.Borrowers.FindAsync([borrowerId], ct)
            ?? throw new KeyNotFoundException($"Borrower {borrowerId} not found.");

        // Find or create a LoanFile for this borrower
        var loanFile = await _db.LoanFiles
            .Where(lf => lf.BorrowerId == borrowerId && lf.IsActive)
            .FirstOrDefaultAsync(ct);

        if (loanFile is null)
        {
            loanFile = new LoanFile
            {
                BorrowerId  = borrower.Id,
                LoanNumber  = borrower.LoanNumber,
                FolderPath  = borrower.FolderName,
                IsActive    = true,
                CreatedBy   = "system",
                UpdatedBy   = "system"
            };
            _db.LoanFiles.Add(loanFile);
            await _db.SaveChangesAsync(ct);
        }

        // Step 1: Rename in-place (PENDING_DocType_Date.pdf → LastName,FirstName_DocType_Date.pdf)
        var renamedPath = await _renamer.RenameFileAsync(document, borrower, ct);
        document.RenamedFileName = Path.GetFileName(renamedPath);
        document.SourcePath      = renamedPath;
        document.UpdatedBy       = "system";
        document.UpdatedDate     = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Step 2: Route to borrower folder
        var filedPath = await _router.RouteAsync(document, borrower, ct);
        document.LoanFileId  = loanFile.Id;
        document.FiledPath   = filedPath;
        document.SourcePath  = filedPath;
        document.Status      = DocumentStatus.Filed;
        document.UpdatedBy   = "system";
        document.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await LogAsync(document.Id, document.RenamedFileName, "AssignBorrower", "Success",
            $"Assigned to {borrower.FolderName} — filed to {filedPath}", AppLogLevel.Info, ct);

        _logger.LogInformation("Document {Id} assigned to borrower {BorrowerId} — filed: {Path}",
            documentId, borrowerId, filedPath);
    }

    private async Task LogAsync(int? documentId, string fileName, string action,
        string outcome, string message, AppLogLevel level, CancellationToken ct)
    {
        _db.ProcessingLogs.Add(new ProcessingLog
        {
            DocumentId  = documentId,
            FileName    = fileName,
            Action      = action,
            Outcome     = outcome,
            Message     = message,
            Level       = level,
            CreatedBy   = "system",
            UpdatedBy   = "system"
        });
        await _db.SaveChangesAsync(ct);
    }
}
