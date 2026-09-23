namespace DocImageSort.Api.Models;

/// <summary>
/// Represents an active loan file for a borrower.
/// Groups all documents filed under a single borrower folder.
/// A LoanFile is created on demand the first time a document is assigned to a borrower.
/// </summary>
public class LoanFile : BaseEntity
{
    /// <summary>Foreign key to the primary borrower.</summary>
    public int BorrowerId { get; set; }

    /// <summary>Navigation property to the primary borrower.</summary>
    public Borrower Borrower { get; set; } = null!;

    /// <summary>Loan number copied from the borrower at the time the loan file was created.</summary>
    public string LoanNumber { get; set; } = string.Empty;

    /// <summary>Relative subfolder path under the FilesFolder (e.g. <c>Smith,John</c>).</summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>True while the loan is open and accepting new documents.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Documents filed under this loan file.</summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
