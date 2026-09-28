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
        var borrower = new Borrower { LastName = "Smith", FirstName = "John", LoanNumber = "L12345" };
        var expectedDate = DateTime.UtcNow.ToString("MMddyy");

        var result = _sut.GenerateFileName(doc, borrower);

        Assert.Equal($"L12345_Pay_Stub_{expectedDate}.pdf", result);
    }

    [Fact]
    public void GenerateFileName_WithoutBorrower_ReturnsPendingPrefix()
    {
        var doc = new Document
        {
            DocumentType = "Pay Stub",
            DocumentDate = new DateTime(2026, 1, 15)
        };

        var result = _sut.GenerateFileName(doc, borrower: null);

        Assert.StartsWith("PENDING_", result);
        Assert.Contains("Pay_Stub", result);
    }

    [Fact]
    public void GenerateFileName_WithoutDocumentDate_UsesCurrentDate()
    {
        var doc = new Document { DocumentType = "Bank Statement", DocumentDate = null };
        var expectedDate = DateTime.UtcNow.ToString("MMddyy");

        var result = _sut.GenerateFileName(doc);

        Assert.Equal($"PENDING_Bank_Statement_{expectedDate}.pdf", result);
    }

    [Fact]
    public void GenerateFileName_BorrowerNameWithInvalidChars_SanitizesLoanNumber()
    {
        var doc = new Document
        {
            DocumentType = "Pay Stub",
            DocumentDate = new DateTime(2026, 6, 1)
        };
        // Loan number with chars outside [A-Za-z0-9_] should be stripped.
        var borrower = new Borrower { LastName = "O'Brien", FirstName = "Jean/Paul", LoanNumber = "L-12345" };

        var result = _sut.GenerateFileName(doc, borrower);

        var invalidChars = Path.GetInvalidFileNameChars();
        Assert.DoesNotContain(result, c => invalidChars.Contains(c));
        Assert.StartsWith("L12345_", result);
    }

    [Fact]
    public void GenerateFileName_NullDocumentType_UsesUnknown()
    {
        var doc = new Document { DocumentType = null!, DocumentDate = new DateTime(2026, 3, 1) };

        var result = _sut.GenerateFileName(doc);

        Assert.Contains("Unknown", result);
    }

    [Fact]
    public void GenerateFileName_W2_EmbedsTaxYearInTypePart()
    {
        var doc = new Document
        {
            DocumentType = "W2",
            DocumentDate = new DateTime(2024, 1, 1)
        };
        var borrower = new Borrower { LoanNumber = "L99999" };
        var expectedDate = DateTime.UtcNow.ToString("MMddyy");

        var result = _sut.GenerateFileName(doc, borrower);

        Assert.Equal($"L99999_W2_24_{expectedDate}.pdf", result);
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
        var borrower = new Borrower { LastName = "Jones", FirstName = "Mary", LoanNumber = "L55555" };
        var expectedName = $"L55555_Pay_Stub_{DateTime.UtcNow:MMddyy}.pdf";

        string? result = null;
        try
        {
            result = await _sut.RenameFileAsync(doc, borrower);

            Assert.True(File.Exists(result));
            Assert.Equal(expectedName, Path.GetFileName(result));
            Assert.False(File.Exists(sourcePath));
        }
        finally
        {
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
            if (result != null && File.Exists(result)) File.Delete(result);
        }
    }

    [Fact]
    public async Task RenameFileAsync_DuplicateTargetName_AppendsCounter()
    {
        var tempDir = Path.GetTempPath();
        var sourcePath   = Path.Combine(tempDir, $"src_{Guid.NewGuid()}.pdf");
        var expectedDate = DateTime.UtcNow.ToString("MMddyy");
        var existingPath = Path.Combine(tempDir, $"PENDING_Pay_Stub_{expectedDate}.pdf");

        await File.WriteAllTextAsync(sourcePath, "src");
        await File.WriteAllTextAsync(existingPath, "existing");

        var doc = new Document
        {
            DocumentType = "Pay Stub",
            DocumentDate = new DateTime(2026, 8, 1),
            SourcePath = sourcePath
        };

        string? result = null;
        try
        {
            result = await _sut.RenameFileAsync(doc, borrower: null);

            Assert.True(File.Exists(result));
            Assert.NotEqual(existingPath, result);
            Assert.True(File.Exists(existingPath));
        }
        finally
        {
            if (File.Exists(sourcePath))  File.Delete(sourcePath);
            if (File.Exists(existingPath)) File.Delete(existingPath);
            if (result != null && File.Exists(result)) File.Delete(result);
        }
    }
}
