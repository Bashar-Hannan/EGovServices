using EGovServices.Application.Features.Verification;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
namespace EGovServices.API.Controllers;

/// <summary>
/// Endpoint عام للتحقق من صحة الوثائق.
/// لا يحتاج تسجيل دخول — الجهات الخارجية تستخدمه مباشرة.
///
/// GET /api/verification/document?token=xK9mP2vQ...
/// </summary>
[ApiController]
[Route("api/verification")]
public sealed class VerificationController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// التحقق من صحة وثيقة عبر الـ Token المضمَّن في الـ QR Code.
    ///
    /// مثال:
    /// GET /api/verification/document?token=xK9mP2vQrT8nL1wY4jH6bF3sA0eN5cU7dZi
    ///
    /// رد ناجح:
    /// {
    ///   "isValid": true,
    ///   "referenceNumber": "REQ-2026-000001",
    ///   "documentType": "شهادة عدم المحكومية",
    ///   "maskedCitizenName": "أحم** م****",
    ///   "maskedNationalNumber": "123****890",
    ///   "issuedAt": "10/05/2026",
    ///   "status": "✅ وثيقة صحيحة وسارية",
    ///   "expiresAt": null
    /// }
    /// </summary>
    [HttpGet("document")]
    [AllowAnonymous]
    [EnableRateLimiting("VerificationPolicy")]   // ← Rate Limiting لمنع Brute Force
    public async Task<IActionResult> VerifyDocument(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "رمز التحقق مطلوب" });

        var result = await mediator.Send(
            new VerifyDocumentQuery(token),
            cancellationToken);

        // دائماً 200 — حتى عند الفشل، لا نكشف معلومات للمهاجم
        return Ok(result.Value);
    }
}
