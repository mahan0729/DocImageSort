using DocImageSort.Api.Data;
using DocImageSort.Api.Models;
using DocImageSort.Api.Services;
using DocImageSort.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DocImageSort.Tests.Pipeline;

/// <summary>
/// End-to-end regression tests for the document pipeline.
/// Uses real file system operations, real rename/routing/duplicate services,
/// and mocked AI classification (no Anthropic API key required).
/// Each test gets its own isolated temp directories and InMemory database.
/// </summary>
public class DocumentPipelineServiceTests : IDisposable
{
    private readonly string _dropFolder;
    private readonly string _filesFolder;
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IClassificationService> _classifier;
    private readonly DocumentPipelineService _sut;

    public DocumentPipelineServiceTests()
    {
        var testId = Guid.NewGuid().ToString("N");
        var dbName = $"PipelineTestDb_{testId}";

        _dropFolder  = Path.Combine(Path.GetTempPath(), $"DocDrop_{testId}");
        _filesFolder = Path.Combine(Path.GetTempPath(), $"DocFiles_{testId}");
        Directory.CreateDirectory(_dropFolder);
        Directory.CreateDirectory(_filesFolder);

        // ServiceProvider provides IServiceScopeFactory + AppDbContext to the pipeline.
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        _serviceProvider = services.BuildServiceProvider();

        // Separate context for DuplicateDetectionService — shares the same InMemory database.
        var ddDb = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FilesFolder:Path"] = _filesFolder
            })
            .Build();

        _classifier = new Mock<IClassificationService>();
        DefaultClassifier("Pay Stub", "2026-08");

        _sut = new DocumentPipelineService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            _classifier.Object,
            new ConversionService(NullLogger<ConversionService>.Instance),
            new RenameService(NullLogger<RenameService>.Instance),
            new RoutingService(config, NullLogger<RoutingService>.Instance),
            new DuplicateDetectionService(ddDb, NullLogger<DuplicateDetectionService>.Instance),
            NullLogger<DocumentPipelineService>.Instance);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        if (Directory.Exists(_dropFolder))  Directory.Delete(_dropFolder,  recursive: true);
        if (Directory.Exists(_filesFolder)) Directory.Delete(_filesFolder, recursive: true);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessFile_ValidPdf_DocumentCreatedAndFiledToPending()
    {
        var filePath = await DropFileAsync("paystub.pdf");

        await _sut.ProcessFileAsync(filePath);

        var doc = await SingleDocumentAsync();
        Assert.Equal(DocumentStatus.Filed, doc.Status);
        Assert.Contains("PENDING", doc.FiledPath);
        Assert.True(File.Exists(doc.FiledPath));
    }

    [Fact]
    public async Task ProcessFile_ValidPdf_DocumentTypeAndDateSetFromClassifier()
    {
        DefaultClassifier("W-2", "2025-12");
        var filePath = await DropFileAsync("w2.pdf");

        await _sut.ProcessFileAsync(filePath);

        var doc = await SingleDocumentAsync();
        Assert.Equal("W-2", doc.DocumentType);
        Assert.NotNull(doc.DocumentDate);
    }

    [Fact]
    public async Task ProcessFile_ValidPdf_RenamedFileUsesDocTypeAndDate()
    {
        DefaultClassifier("Bank Statement", "2026-07");
        var filePath = await DropFileAsync("bank.pdf");

        await _sut.ProcessFileAsync(filePath);

        var doc = await SingleDocumentAsync();
        Assert.Contains("Bank_Statement", doc.RenamedFileName);
        Assert.StartsWith("PENDING_", doc.RenamedFileName);
    }

    [Fact]
    public async Task ProcessFile_ClassificationFails_StillFilesWithUnknownType()
    {
        _classifier
            .Setup(c => c.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClassificationResult("Unknown", null, "API key not configured.", false));

        var filePath = await DropFileAsync("unknown.pdf");

        await _sut.ProcessFileAsync(filePath);

        var doc = await SingleDocumentAsync();
        Assert.Equal(DocumentStatus.Filed, doc.Status);
        Assert.Equal("Unknown", doc.DocumentType);
    }

    // ── Duplicate detection ───────────────────────────────────────────────────

    [Fact]
    public async Task ProcessFile_SameFileDroppedTwice_SecondFlaggedAsDuplicate()
    {
        var content = "identical pdf content for duplicate test";
        var first  = await DropFileAsync("doc1.pdf", content);
        var second = await DropFileAsync("doc2.pdf", content);

        await _sut.ProcessFileAsync(first);
        await _sut.ProcessFileAsync(second);

        var docs = await AllDocumentsAsync();
        Assert.Equal(2, docs.Count);
        Assert.Equal(DocumentStatus.Filed,     docs.Single(d => d.OriginalFileName == "doc1.pdf").Status);
        Assert.Equal(DocumentStatus.Duplicate, docs.Single(d => d.OriginalFileName == "doc2.pdf").Status);
    }

    [Fact]
    public async Task ProcessFile_SameFileDroppedTwice_DuplicateHasNoFiledPath()
    {
        var content = "duplicate content check";
        var first  = await DropFileAsync("a.pdf", content);
        var second = await DropFileAsync("b.pdf", content);

        await _sut.ProcessFileAsync(first);
        await _sut.ProcessFileAsync(second);

        var duplicate = (await AllDocumentsAsync()).Single(d => d.Status == DocumentStatus.Duplicate);
        Assert.Empty(duplicate.FiledPath);
    }

    [Fact]
    public async Task ProcessFile_DifferentContent_BothFiled()
    {
        var first  = await DropFileAsync("x.pdf", "content alpha");
        var second = await DropFileAsync("y.pdf", "content beta");

        await _sut.ProcessFileAsync(first);
        await _sut.ProcessFileAsync(second);

        var docs = await AllDocumentsAsync();
        Assert.Equal(2, docs.Count);
        Assert.All(docs, d => Assert.Equal(DocumentStatus.Filed, d.Status));
    }

    // ── File type filtering ───────────────────────────────────────────────────

    [Fact]
    public async Task ProcessFile_UnsupportedExtension_NoDocumentCreated()
    {
        var filePath = Path.Combine(_dropFolder, "readme.txt");
        await File.WriteAllTextAsync(filePath, "not a document");

        await _sut.ProcessFileAsync(filePath);

        var docs = await AllDocumentsAsync();
        Assert.Empty(docs);
    }

    [Fact]
    public async Task ProcessFile_PdfExtension_IsSupported()
    {
        var filePath = await DropFileAsync("supported.pdf");
        await _sut.ProcessFileAsync(filePath);
        Assert.Single(await AllDocumentsAsync());
    }

    [Fact]
    public async Task ProcessFile_JpgExtension_IsSupported()
    {
        // ConversionService passes .jpg through if XImage fails — pipeline should not throw.
        var filePath = await DropFileAsync("scan.jpg");
        await _sut.ProcessFileAsync(filePath);
        // A document record should exist regardless of whether conversion succeeded.
        Assert.NotEmpty(await AllDocumentsAsync());
    }

    // ── Processing log ────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessFile_ValidPdf_LogsIngestAction()
    {
        var filePath = await DropFileAsync("log_test.pdf");
        await _sut.ProcessFileAsync(filePath);
        Assert.True(await HasLogActionAsync("Ingest"));
    }

    [Fact]
    public async Task ProcessFile_ValidPdf_LogsDuplicateCheckAction()
    {
        var filePath = await DropFileAsync("log_dup.pdf");
        await _sut.ProcessFileAsync(filePath);
        Assert.True(await HasLogActionAsync("DuplicateCheck"));
    }

    [Fact]
    public async Task ProcessFile_ValidPdf_LogsClassifyAction()
    {
        var filePath = await DropFileAsync("log_classify.pdf");
        await _sut.ProcessFileAsync(filePath);
        Assert.True(await HasLogActionAsync("Classify"));
    }

    [Fact]
    public async Task ProcessFile_ValidPdf_LogsRenameAction()
    {
        var filePath = await DropFileAsync("log_rename.pdf");
        await _sut.ProcessFileAsync(filePath);
        Assert.True(await HasLogActionAsync("Rename"));
    }

    [Fact]
    public async Task ProcessFile_ValidPdf_LogsRouteAction()
    {
        var filePath = await DropFileAsync("log_route.pdf");
        await _sut.ProcessFileAsync(filePath);
        Assert.True(await HasLogActionAsync("Route"));
    }

    [Fact]
    public async Task ProcessFile_Duplicate_LogsDuplicateCheckWithDuplicateOutcome()
    {
        var content = "dup log content";
        await _sut.ProcessFileAsync(await DropFileAsync("first.pdf", content));
        await _sut.ProcessFileAsync(await DropFileAsync("second.pdf", content));

        var db = FreshDb();
        var dupLog = await db.ProcessingLogs
            .Where(l => l.Action == "DuplicateCheck" && l.Outcome == "Duplicate")
            .FirstOrDefaultAsync();

        Assert.NotNull(dupLog);
        Assert.Equal(DocImageSort.Api.Models.LogLevel.Warning, dupLog.Level);
    }

    // ── Multiple files ────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessFile_ThreeUniqueFiles_AllFiledIndependently()
    {
        await _sut.ProcessFileAsync(await DropFileAsync("one.pdf",   "content 1"));
        await _sut.ProcessFileAsync(await DropFileAsync("two.pdf",   "content 2"));
        await _sut.ProcessFileAsync(await DropFileAsync("three.pdf", "content 3"));

        var docs = await AllDocumentsAsync();
        Assert.Equal(3, docs.Count);
        Assert.All(docs, d => Assert.Equal(DocumentStatus.Filed, d.Status));
    }

    [Fact]
    public async Task ProcessFile_ThreeUniqueFiles_EachHasUniqueFiledPath()
    {
        await _sut.ProcessFileAsync(await DropFileAsync("p1.pdf", "alpha"));
        await _sut.ProcessFileAsync(await DropFileAsync("p2.pdf", "beta"));
        await _sut.ProcessFileAsync(await DropFileAsync("p3.pdf", "gamma"));

        var paths = (await AllDocumentsAsync()).Select(d => d.FiledPath).ToList();
        Assert.Equal(paths.Count, paths.Distinct().Count());
    }

    // ── File hash ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessFile_ValidPdf_FileHashStoredOnDocument()
    {
        var filePath = await DropFileAsync("hashtest.pdf");
        await _sut.ProcessFileAsync(filePath);

        var doc = await SingleDocumentAsync();
        Assert.NotEmpty(doc.FileHash);
        Assert.Equal(64, doc.FileHash.Length); // SHA-256 = 64 hex chars
    }

    // ── Qualifier logic for new doc types ────────────────────────────────────

    [Theory]
    [InlineData("Divorce Decree")]
    [InlineData("Bankruptcy (Chapter 7)")]
    [InlineData("Bankruptcy (Chapter 13)")]
    [InlineData("Child Support Order")]
    [InlineData("Alimony Agreement")]
    public async Task ProcessFile_LegalDocWithSubjectName_QualifierIsSubjectName(string docType)
    {
        _classifier
            .Setup(c => c.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClassificationResult(docType, "2024-01", "Legal document.", true,
                SubjectName: "Jane Doe"));

        var filePath = await DropFileAsync($"{docType.Replace(" ", "_")}.pdf");
        await _sut.ProcessFileAsync(filePath);

        var doc = await SingleDocumentAsync();
        Assert.Equal("Jane Doe", doc.DocumentQualifier);
    }

    [Theory]
    [InlineData("Retirement Statement")]
    [InlineData("Investment Account Statement")]
    public async Task ProcessFile_FinancialStatementWithInstitution_QualifierIsInstitutionAndAccountType(string docType)
    {
        _classifier
            .Setup(c => c.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClassificationResult(docType, "2026-01", "Statement.", true,
                AccountType: "401K", InstitutionName: "Fidelity"));

        var filePath = await DropFileAsync("retirement.pdf");
        await _sut.ProcessFileAsync(filePath);

        var doc = await SingleDocumentAsync();
        Assert.Contains("Fidelity", doc.DocumentQualifier);
        Assert.Contains("401K", doc.DocumentQualifier);
    }

    [Fact]
    public async Task ProcessFile_UnknownDocWithPrintedTitle_DocumentTypeIsTitleText()
    {
        _classifier
            .Setup(c => c.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClassificationResult("Notice Of Default", "2026-03", "Title read from doc.", true));

        var filePath = await DropFileAsync("notice.pdf");
        await _sut.ProcessFileAsync(filePath);

        var doc = await SingleDocumentAsync();
        Assert.Equal("Notice Of Default", doc.DocumentType);
        Assert.Contains("Notice_Of_Default", doc.RenamedFileName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void DefaultClassifier(string docType, string docDate) =>
        _classifier
            .Setup(c => c.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClassificationResult(docType, docDate, "Test classification.", true));

    private async Task<string> DropFileAsync(string fileName, string content = "mock pdf content")
    {
        var path = Path.Combine(_dropFolder, fileName);
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    private AppDbContext FreshDb()
    {
        var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private async Task<Document> SingleDocumentAsync()
    {
        var docs = await FreshDb().Documents.ToListAsync();
        return Assert.Single(docs);
    }

    private async Task<List<Document>> AllDocumentsAsync() =>
        await FreshDb().Documents.ToListAsync();

    private async Task<bool> HasLogActionAsync(string action) =>
        await FreshDb().ProcessingLogs.AnyAsync(l => l.Action == action);
}
