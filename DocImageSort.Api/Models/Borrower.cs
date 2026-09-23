namespace DocImageSort.Api.Models;

/// <summary>
/// Represents a mortgage borrower. Folder naming: LastName,FirstName.
/// </summary>
public class Borrower : BaseEntity
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LoanNumber { get; set; } = string.Empty;
    public bool IsPrimaryBorrower { get; set; } = true;

    /// <summary>Derived folder name: LastName,FirstName</summary>
    public string FolderName => $"{LastName},{FirstName}";

    public ICollection<LoanFile> LoanFiles { get; set; } = new List<LoanFile>();
}
