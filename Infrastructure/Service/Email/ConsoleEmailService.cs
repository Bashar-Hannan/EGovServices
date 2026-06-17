using EGovServices.Application.Common.Interfaces;

namespace EGovServices.Infrastructure.Service.Email;

/// <summary>
/// Development-only email service.
/// Prints OTP to the console instead of sending a real email.
///
/// HOW TO USE IN SWAGGER:
/// 1. Run the project → a console window opens
/// 2. Call POST /api/auth/register
/// 3. Look at the console for the OTP code
/// 4. Use it in POST /api/auth/verify-otp
///
/// Switched to MailKitEmailService in Production via Program.cs.
/// </summary>
public sealed class ConsoleEmailService : IEmailService
{
    public Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
    {
        // ── Print a clear, visible block in the console ───────────────
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║        [DEV] OTP EMAIL SIMULATION   ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        Console.WriteLine($"║  To:      {toEmail,-26} ║");
        Console.WriteLine($"║  Name:    {fullName,-26} ║");
        Console.WriteLine($"║  OTP:     {otpCode,-26} ║");
        Console.WriteLine($"║  Expires: {DateTime.UtcNow.AddMinutes(10):HH:mm:ss} UTC          ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        return Task.CompletedTask;
    }
}
