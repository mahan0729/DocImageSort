using System.ComponentModel.DataAnnotations.Schema;

namespace DocImageSort.Api.Models;

/// <summary>
/// Application user. Phase 1: seeded for reference only — no auth flow.
/// Phase 2 will add login, password reset, and role-based access.
/// </summary>
public class User : BaseEntity
{
    /// <summary>User's first name.</summary>
    [Column(Order = 1)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>User's last name.</summary>
    [Column(Order = 2)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Unique email address. Used as the login identifier in Phase 2.</summary>
    [Column(Order = 3)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Short login handle.</summary>
    [Column(Order = 4)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Bcrypt password hash. Nullable — not used in Phase 1.
    /// Set when the user establishes a password in Phase 2.
    /// </summary>
    [Column(Order = 5)]
    public string? PasswordHash { get; set; }

    /// <summary>Role assigned to this user (e.g. "Admin", "User").</summary>
    [Column(Order = 6)]
    public string Role { get; set; } = "User";

    /// <summary>False when the account has been deactivated.</summary>
    [Column(Order = 7)]
    public bool IsActive { get; set; } = true;

    /// <summary>Derived display name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
