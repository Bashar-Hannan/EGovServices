namespace EGovServices.Domain.Entities;

/// <summary>
/// Stores OTP codes AND temporary registration data.
/// The registration data is saved here in Step 1 (Register)
/// so Step 2 (VerifyOtp) only needs NationalNumber + OtpCode.
/// </summary>
public class OtpVerification
{
    public required Guid Id { get; set; }
    public required string NationalNumber { get; set; }
    public required string OtpCode { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public required bool IsUsed { get; set; }
    public required DateTime CreatedAt { get; set; }
    public int Attempts { get; set; }

    // ── Temporary registration data (saved in Step 1) ─────────────
    // Retrieved in Step 2 to create the User without re-asking the citizen

    /// <summary>Saved from Register request — used to create User</summary>
    public required string TempPhoneNumber { get; set; }

    /// <summary>Saved from Register request — used to create User + send emails</summary>
    public required string TempEmail { get; set; }

    /// <summary>
    /// SHA256 hash of the password — already hashed before saving.
    /// Raw password is never persisted anywhere.
    /// </summary>
    public required string TempPasswordHash { get; set; }
}
