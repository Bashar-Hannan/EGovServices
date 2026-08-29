using System.Security.Claims;

namespace EGovServices.Application.Features.MedicalFile;

/// <summary>
/// Part 2: Helper methods — نفس نمط CreateClearanceCertificateHandler_Part2 بالضبط.
/// </summary>
public sealed partial class CreateMedicalFileHandler
{
    /// <summary>
    /// يستخرج NationalNumber من الـ JWT claim المخصص "NationalNumber"
    /// (نفس claim المستخدم في شهادة عدم المحكومية — JwtService.cs).
    /// </summary>
    private string? ExtractNationalNumberFromClaims()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null) return null;

        var nationalNumber = user.FindFirst("NationalNumber")?.Value;
        return string.IsNullOrWhiteSpace(nationalNumber) ? null : nationalNumber;
    }

    /// <summary>يستخرج UserId (Guid) من الـ JWT للتحقق من ملكية الطلب.</summary>
    private Guid? ExtractUserId()
    {
        var claim = httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
