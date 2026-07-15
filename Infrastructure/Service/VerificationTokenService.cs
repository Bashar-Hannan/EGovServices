using EGovServices.Application.Common.Interfaces;
using System.Security.Cryptography;

namespace EGovServices.Infrastructure.Service;

/// <summary>
/// تنفيذ IVerificationTokenService.
/// بعد التبسيط: يولّد Token فقط — لا URL، لا Frontend.
/// الـ Token نفسه يُضمَّن في QR Code مباشرة.
/// </summary>
public sealed class VerificationTokenService : IVerificationTokenService
{
    /// <summary>
    /// 32 byte عشوائي → Base64 URL-safe (43 حرف، بدون +, /, =)
    /// آمن تشفيرياً (Cryptographically Secure).
    /// </summary>
    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
