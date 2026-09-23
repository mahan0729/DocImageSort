namespace DocImageSort.Api.Models;

/// <summary>
/// A loan file belonging to a primary borrower.
/// </summary>
public class LoanFile : BaseEntity
{
    public int BorrowerId { get; set; }
    public Borrower Borrower { get; set; } = null!;

    public string LoanNumber { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
