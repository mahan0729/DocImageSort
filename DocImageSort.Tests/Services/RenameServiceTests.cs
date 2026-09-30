using DocImageSort.Api.Models;
using DocImageSort.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocImageSort.Tests.Services;

public class RenameServiceTests
{
    private readonly RenameService _sut = new(NullLogger<RenameService>.Instance);

    // ── GenerateFileName ──────────────────────────────────────────────────────

    [Fact]
    public void GenerateFileName_WithBorrower_UsesLastNameFirstNamePrefix()
    {
        var doc = new Document
        {
            DocumentType = "Pay Stub",
            DocumentDate = new DateTime(2026, 8, 1)
        };
        var borrower = new Borrower { LastName = "Smith", FirstName = "John", LoanNumber = "L12345" };

        var result = _sut.GenerateFileName(doc, borrower);

        Assert.StartsWith("Smith_John_Pay_Stub_", result);
        Assert.EndsWith(".pdf", result);
    }

    [Fact]
    public void GenerateFileName_WithBorrower_UsesFourDigitYear()
    {
        var doc = new Document
        {
            DocumentType = "Pay Stub",
            DocumentDate = new DateTime(2026, 8, 1)
        };
        var borrower = new Borrower { LastName = "Smith", FirstName = "John", LoanNumber = "L12345" };

        var result = _sut.GenerateFileName(doc, borrower);

        // Date part should be MMDDYYYY (8 digits), not MMDDYY (6 digits)
        Assert.Equal("Smith_John_Pay_Stub_08012026.pdf", result);
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
    public void GenerateFileName_WithoutDocumentDate_UsesCurrentDateFourDigitYear()
    {
        var doc = new Document { DocumentType = "Bank Statement", DocumentDate = null };
        var expectedDate = DateTime.UtcNow.ToString("MMddyyyy");

        var result = _sut.GenerateFileName(doc);

        Assert.Equal($"PENDING_Bank_Statement_{expectedDate}.pdf", result);
    }

    [Fact]
    public void GenerateFileName_BorrowerNameWithInvalidChars_SanitizesName()
    {
        var doc = new Document
        {
            DocumentType = "Pay Stub",
            DocumentDate = new DateTime(2026, 6, 1)
        };
        var borrower = new Borrower { LastName = "O'Brien", FirstName = "Jean/Paul", LoanNumber = "L-12345" };

        var result = _sut.GenerateFileName(doc, borrower);

        var invalidChars = Path.GetInvalidFileNameChars();
        Assert.DoesNotContain(result, c => invalidChars.Contains(c));
        Assert.StartsWith("OBrien_JeanPaul_", result);
    }

    [Fact]
    public void GenerateFileName_NullDocumentType_UsesUnknown()
    {
        var doc = new Document { DocumentType = null!, DocumentDate = new DateTime(2026, 3, 1) };

        var result = _sut.GenerateFileName(doc);

        Assert.Contains("Unknown", result);
    }

    [Fact]
    public void GenerateFileName_W2_EmbedsFourDigitTaxYear()
    {
        var doc = new Document
        {
            DocumentType = "W2",
            DocumentDate = new DateTime(2024, 1, 1)
        };
        var borrower = new Borrower { LastName = "Jones", FirstName = "Mary", LoanNumber = "L99999" };

        var result = _sut.GenerateFileName(doc, borrower);

        Assert.Equal("Jones_Mary_W2_2024_01012024.pdf", result);
    }

    [Fact]
    public void GenerateFileName_1099Composite_EmbedsTaxYear()
    {
        var doc = new Document
        {
            DocumentType = "1099 Composite",
            DocumentDate = new DateTime(2024, 1, 1)
        };
        var borrower = new Borrower { LastName = "Davis", FirstName = "Kim", LoanNumber = "L11111" };

        var result = _sut.GenerateFileName(doc, borrower);

        Assert.Equal("Davis_Kim_1099_Composite_2024_01012024.pdf", result);
    }

    [Theory]
    [InlineData("1099-INT")]
    [InlineData("1099-DIV")]
    [InlineData("1099-B")]
    [InlineData("1099-MISC")]
    [InlineData("1099-NEC")]
    [InlineData("1099 Composite")]
    public void GenerateFileName_All1099Types_EmbedTaxYear(string docType)
    {
        var doc = new Document
        {
            DocumentType = docType,
            DocumentDate = new DateTime(2024, 3, 15)
        };
        var borrower = new Borrower { LastName = "Test", FirstName = "User", LoanNumber = "L00001" };

        var result = _sut.GenerateFileName(doc, borrower);

        Assert.Contains("2024", result);
        Assert.EndsWith(".pdf", result);
    }

    [Fact]
    public void GenerateFileName_BankStatement_WithDateRange_UsesStartToEndFormat()
    {
        var doc = new Document
        {
            DocumentType = "Bank Statement",
            DocumentDate    = new DateTime(2026, 1, 1),
            DocumentEndDate = new DateTime(2026, 1, 31)
        };
        var borrower = new Borrower { LastName = "Smith", FirstName = "John", LoanNumber = "L12345" };

        var result = _sut.GenerateFileName(doc, borrower);

        Assert.Contains("01012026to01312026", result);
    }

    [Fact]
    public void GenerateFileName_BankStatement_NoEndDate_UsesSingleDate()
    {
        var doc = new Document
        {
            DocumentType    = "Bank Statement",
            DocumentDate    = new DateTime(2026, 3, 15),
            DocumentEndDate = null
        };
        var borrower = new Borrower { LastName = "Smith", FirstName = "John", LoanNumber = "L12345" };

        var result = _sut.GenerateFileName(doc, borrower);

        Assert.DoesNotContain("to", result);
        Assert.Contains("03152026", result);
    }

    [Fact]
    public void GenerateFileName_BankStatement_EndDateSameAsStartDate_UsesSingleDate()
    {
        var date = new DateTime(2026, 3, 15);
        var doc = new Document
        {
            DocumentType    = "Bank Statement",
            DocumentDate    = date,
            DocumentEndDate = date
        };

        var result = _sut.GenerateFileName(doc);

        Assert.DoesNotContain("to", result);
        Assert.Contains("03152026", result);
    }

    // ── RenameFileAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RenameFileAsync_FileExists_RenamesWithBorrowerName()
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
        var expectedName = "Jones_Mary_Pay_Stub_08012026.pdf";

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
        var expectedDate = DateTime.UtcNow.ToString("MMddyyyy");
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
