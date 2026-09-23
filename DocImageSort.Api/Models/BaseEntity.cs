using System.ComponentModel.DataAnnotations.Schema;

namespace DocImageSort.Api.Models;

/// <summary>
/// Base class for all database entities.
/// Column ordering standard: PK at order 0, domain columns at orders 1–99, audit columns at orders 100–103.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Auto-incremented surrogate primary key.</summary>
    [Column(Order = 0)]
    public int Id { get; set; }

    /// <summary>Username or process that created the record.</summary>
    [Column(Order = 100)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the record was created.</summary>
    [Column(Order = 101)]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>Username or process that last updated the record.</summary>
    [Column(Order = 102)]
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the most recent update.</summary>
    [Column(Order = 103)]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
