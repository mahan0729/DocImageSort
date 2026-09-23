using System.ComponentModel.DataAnnotations.Schema;

namespace DocImageSort.Api.Models;

/// <summary>
/// A mortgage borrower. Each borrower gets their own subfolder under the FilesFolder.
/// Folder naming convention: <c>LastName,FirstName</c> (comma-separated, no space).
/// </summary>
public class Borrower : BaseEntity
{
    /// <summary>Borrower's last name. Used as the first segment of the folder name.</summary>
    [Column(Order = 1)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Borrower's first name. Used as the second segment of the folder name.</summary>
    [Column(Order = 2)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Lender's loan number for this borrower's file.</summary>
    [Column(Order = 3)]
    public string LoanNumber { get; set; } = string.Empty;

    /// <summary>
    /// True if this is the primary borrower on the loan.
    /// Folder and file names use the primary borrower only.
    /// </summary>
    [Column(Order = 4)]
    public bool IsPrimaryBorrower { get; set; } = true;

    /// <summary>Derived folder name in the format <c>LastName,FirstName</c>.</summary>
    public string FolderName => $"{LastName},{FirstName}";

    /// <summary>Loan files associated with this borrower.</summary>
    public ICollection<LoanFile> LoanFiles { get; set; } = new List<LoanFile>();
}
