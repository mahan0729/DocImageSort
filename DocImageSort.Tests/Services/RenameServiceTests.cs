using DocImageSort.Api.Models;
using DocImageSort.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocImageSort.Tests.Services;

public class RenameServiceTests
{
    private readonly RenameService _sut = new(NullLogger<RenameService>.Instance);

    // ── GenerateFileName ──────────────────────────────────────────────────────

    [Fact]
    public void GenerateFileName_WithBorrower_ReturnsFormattedName()
    {
        var doc = new Document
        {
            DocumentType = "Pay Stub",
            DocumentDate = new DateTime(2026, 8, 1)
        };
        var borrower = new Borrower { LastName = "Smith", FirstName = "John" };

        var result = _sut.GenerateFileName(doc, borrower);

        Assert.Equal("Smith,John_Pay Stub_2026-08.pdf", result);
    }

    [Fact]
    public void GenerateFileName_WithoutBorrower_ReturnsPendingPrefix()
    {
        var doc = new Document
        {
            DocumentType = "W-2",
            DocumentDate = new DateTime(2026, 1, 15)
        };

        var result = _sut.GenerateFileName(doc, borrower: null);

        Assert.StartsWith("PENDING_", result);
        Assert.Contains("W-2", result);
    }

    [Fact]
    public void GenerateFileName_WithoutDocumentDate_UsesCurrentMonth()
    {
        var doc = new Document { DocumentType = "Bank Statement", DocumentDate = null };
        var expected = $"PENDING_Bank Statement_{DateTime.UtcNow:yyyy-MM}.pdf";

        var result = _sut.GenerateFileName(doc);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateFileName_BorrowerNameWithInvalidChars_SanitizesName()
    {
        // Characters invalid in file names should be replaced with underscores.
        var doc = new Document
        {
            DocumentType = "Pay Stub",
            DocumentDate = new DateTime(2026, 6, 1)
        };
        var borrower = new Borrower { LastName = "O'Brien", FirstName = "Jean/Paul" };

        var result = _sut.GenerateFileName(doc, borrower);

        // Should not contain any characters invalid in file names.
        var invalidChars = Path.GetInvalidFileNameChars();
        Assert.DoesNotContain(result, c => invalidChars.Contains(c));
    }

    [Fact]
    public void GenerateFileName_NullDocumentType_UsesUnknown()
    {
        var doc = new Document { DocumentType = null!, DocumentDate = new DateTime(2026, 3, 1) };

        var result = _sut.GenerateFileName(doc);

        Assert.Contains("Unknown", result);
    }

    // ── RenameFileAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RenameFileAsync_FileExists_RenamesFile()
    {
        var tempDir = Path.GetTempPath();
        var sourcePath = Path.Combine(tempDir, $"test_rename_{Guid.NewGuid()}.pdf");
        await File.WriteAllTextAsync(sourcePath, "pdf content");

        var doc = new Document
        {
            DocumentType = "Pay Stub",
            DocumentDate = new DateTime(2026, 8, 1),
            SourcePath = sourcePath
        };
        var borrower = new Borrower { LastName = "Jones", FirstName = "Mary" };

        try
        {
            var result = await _sut.RenameFileAsync(doc, borrower);

            Assert.True(File.Exists(result));
            Assert.Equal("Jones,Mary_Pay Stub_2026-08.pdf", Path.GetFileName(result));
            Assert.False(File.Exists(sourcePath));
        }
        finally
        {
            // Clean up whichever file remains.
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
            var renamed = Path.Combine(tempDir, "Jones,Mary_Pay Stub_2026-08.pdf");
            if (File.Exists(renamed)) File.Delete(renamed);
        }
    }

    [Fact]
    public async Task RenameFileAsync_DuplicateTargetName_AppendsCounter()
    {
        var tempDir = Path.GetTempPath();
        var sourcePath  = Path.Combine(tempDir, $"src_{Guid.NewGuid()}.pdf");
        var existingPath = Path.Combine(tempDir, "PENDING_W-2_2026-01.pdf");

        await File.WriteAllTextAsync(sourcePath, "src");
        await File.WriteAllTextAsync(existingPath, "existing");

        var doc = new Document
        {
            DocumentType = "W-2",
            DocumentDate = new DateTime(2026, 1, 1),
            SourcePath = sourcePath
        };

        try
        {
            var result = await _sut.RenameFileAsync(doc, borrower: null);

            // Should not overwrite existing — name gets a counter suffix.
            Assert.True(File.Exists(result));
            Assert.NotEqual(existingPath, result);
            Assert.True(File.Exists(existingPath));
        }
        finally
        {
            if (File.Exists(sourcePath))  File.Delete(sourcePath);
            if (File.Exists(existingPath)) File.Delete(existingPath);
            var counter = Path.Combine(tempDir, "PENDING_W-2_2026-01_1.pdf");
            if (File.Exists(counter)) File.Delete(counter);
        }
    }
}
