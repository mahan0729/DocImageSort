namespace DocImageSort.Api.Models;

/// <summary>
/// Base class for all database entities.
/// Every table has a surrogate primary key and four trailing audit columns.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Auto-incremented surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Username or process that created the record.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the record was created.</summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>Username or process that last updated the record.</summary>
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the most recent update.</summary>
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
