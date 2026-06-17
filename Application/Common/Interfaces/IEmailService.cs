namespace EGovServices.Application.Common.Interfaces;

/// <summary>
/// Contract for sending emails.
/// Two implementations:
/// - ConsoleEmailService (Development) → prints to console, no real email sent
/// - MailKitEmailService (Production)  → sends real email via SMTP
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an OTP verification email.
    /// In Development: prints OTP to console.
    /// In Production: sends HTML email via SMTP.
    /// </summary>
    Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode);
}
