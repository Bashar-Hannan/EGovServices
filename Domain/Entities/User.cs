namespace EGovServices.Domain.Entities;

/// <summary>
/// Updated User entity.
/// Added: Email, IsVerified
/// 
/// Migration note:
/// - Email      → nullable  (old records have no email)
/// - IsVerified → default true (old records treated as pre-verified)
/// </summary>
public class User
{
    public required Guid Id { get; set; }
    public required string NationalNumber { get; set; }
    public required string PhoneNumber { get; set; }
    public required string PasswordHash { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required bool IsActive { get; set; }
    public required string Role { get; set; }

    // ── NEW ──────────────────────────────────────────────────────────

    /// <summary>
    /// Email used during registration and OTP delivery.
    /// Nullable so existing seed-data records are not broken.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// True  = email verified via OTP → can log in.
    /// False = registered but OTP not confirmed yet → login blocked.
    /// Default TRUE in migration so all old records keep working.
    /// </summary>
    public bool IsVerified { get; set; }

    // Navigation
    public Citizen? Citizen { get; set; }
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public Wallet? Wallet { get; set; }
}
