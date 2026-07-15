namespace EGovServices.Application.Common.Interfaces;

/// <summary>
/// Contract لتوليد Verification Tokens.
/// التغيير: حذف BuildVerificationUrl لأن QR يحتوي Token فقط
/// والفلاتر يستدعي API مباشرة بدون موقع ويب وسيط.
/// </summary>
public interface IVerificationTokenService
{
    /// <summary>
    /// يولّد Token عشوائي آمن — 32 byte → Base64 URL-safe (43 حرف)
    /// هذا الـ Token مباشرةً هو ما يُحفظ في QR Code.
    /// </summary>
    string GenerateToken();
}
