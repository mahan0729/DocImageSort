using DocImageSort.Api.Data;
using DocImageSort.Api.Models;
using DocImageSort.Api.Services;
using DocImageSort.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DocImageSort.Tests.Services;

public class DocumentReviewServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRenameService> _renamer;
    private readonly Mock<IRoutingService> _router;
    private readonly DocumentReviewService _sut;

    public DocumentReviewServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _renamer = new Mock<IRenameService>();
        _router  = new Mock<IRoutingService>();

        _sut = new DocumentReviewService(
            _db,
            _renamer.Object,
            _router.Object,
            NullLogger<DocumentReviewService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── CorrectTypeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CorrectTypeAsync_DocumentExists_UpdatesDocumentType()
    {
        var doc = SeedDocument("W-2");

        await _sut.CorrectTypeAsync(doc.Id, "1040 Tax Return");

        var updated = await _db.Documents.FindAsync(doc.Id);
        Assert.Equal("1040 Tax Return", updated!.DocumentType);
    }

    [Fact]
    public async Task CorrectTypeAsync_DocumentExists_WritesProcessingLog()
    {
        var doc = SeedDocument("Unknown");

        await _sut.CorrectTypeAsync(doc.Id, "Pay Stub");

        var log = await _db.ProcessingLogs.FirstOrDefaultAsync(l => l.DocumentId == doc.Id && l.Action == "CorrectType");
        Assert.NotNull(log);
        Assert.Equal("Success", log.Outcome);
    }

    [Fact]
    public async Task CorrectTypeAsync_DocumentNotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.CorrectTypeAsync(documentId: 9999, "Pay Stub"));
    }

    [Fact]
    public async Task CorrectTypeAsync_TrimsWhitespace()
    {
        var doc = SeedDocument("Unknown");

        await _sut.CorrectTypeAsync(doc.Id, "  Bank Statement  ");

        var updated = await _db.Documents.FindAsync(doc.Id);
        Assert.Equal("Bank Statement", updated!.DocumentType);
    }

    // ── AssignBorrowerAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AssignBorrowerAsync_DocumentAndBorrowerExist_CreatesLoanFileAndFilesDocument()
    {
        var doc      = SeedDocument("Pay Stub");
        var borrower = SeedBorrower("Smith", "John", "LN-001");

        _renamer.Setup(r => r.RenameFileAsync(It.IsAny<Document>(), It.IsAny<Borrower>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\DocImageSort\Files\PENDING\Smith,John_Pay Stub_2026-08.pdf");

        _router.Setup(r => r.RouteAsync(It.IsAny<Document>(), It.IsAny<Borrower>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(@"C:\DocImageSort\Files\Smith,John\Smith,John_Pay Stub_2026-08.pdf");

        await _sut.AssignBorrowerAsync(doc.Id, borrower.Id);

        var updated = await _db.Documents.FindAsync(doc.Id);
        Assert.Equal(DocumentStatus.Filed, updated!.Status);
        Assert.NotNull(updated.LoanFileId);
        Assert.Contains("Smith,John", updated.FiledPath);
    }

    [Fact]
    public async Task AssignBorrowerAsync_ExistingLoanFile_ReusesItInsteadOfCreatingNew()
    {
        var borrower  = SeedBorrower("Jones", "Mary", "LN-002");
        var loanFile  = SeedLoanFile(borrower.Id, "LN-002");
        var doc       = SeedDocument("W-2");

        _renamer.Setup(r => r.RenameFileAsync(It.IsAny<Document>(), It.IsAny<Borrower>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"C:\Files\PENDING\Jones,Mary_W-2_2026-01.pdf");
        _router.Setup(r => r.RouteAsync(It.IsAny<Document>(), It.IsAny<Borrower>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(@"C:\Files\Jones,Mary\Jones,Mary_W-2_2026-01.pdf");

        await _sut.AssignBorrowerAsync(doc.Id, borrower.Id);

        var updated = await _db.Documents.FindAsync(doc.Id);
        Assert.Equal(loanFile.Id, updated!.LoanFileId);
        Assert.Single(_db.LoanFiles); // No new LoanFile created.
    }

    [Fact]
    public async Task AssignBorrowerAsync_DocumentNotFound_ThrowsKeyNotFoundException()
    {
        var borrower = SeedBorrower("Test", "User", "LN-000");

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AssignBorrowerAsync(documentId: 9999, borrower.Id));
    }

    [Fact]
    public async Task AssignBorrowerAsync_BorrowerNotFound_ThrowsKeyNotFoundException()
    {
        var doc = SeedDocument("Pay Stub");

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AssignBorrowerAsync(doc.Id, borrowerId: 9999));
    }

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private Document SeedDocument(string docType)
    {
        var doc = new Document
        {
            DocumentType   = docType,
            OriginalFileName = "test.pdf",
            RenamedFileName  = "PENDING_test_2026-08.pdf",
            SourcePath     = @"C:\DocImageSort\Files\PENDING\PENDING_test_2026-08.pdf",
            Status         = DocumentStatus.Pending,
            CreatedBy      = "test",
            UpdatedBy      = "test"
        };
        _db.Documents.Add(doc);
        _db.SaveChanges();
        return doc;
    }

    private Borrower SeedBorrower(string lastName, string firstName, string loanNumber)
    {
        var b = new Borrower
        {
            LastName   = lastName,
            FirstName  = firstName,
            LoanNumber = loanNumber,
            CreatedBy  = "test",
            UpdatedBy  = "test"
        };
        _db.Borrowers.Add(b);
        _db.SaveChanges();
        return b;
    }

    private LoanFile SeedLoanFile(int borrowerId, string loanNumber)
    {
        var lf = new LoanFile
        {
            BorrowerId = borrowerId,
            LoanNumber = loanNumber,
            FolderPath = "Jones,Mary",
            IsActive   = true,
            CreatedBy  = "test",
            UpdatedBy  = "test"
        };
        _db.LoanFiles.Add(lf);
        _db.SaveChanges();
        return lf;
    }
}
