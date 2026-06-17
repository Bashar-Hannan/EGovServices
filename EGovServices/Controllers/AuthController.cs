using EGovServices.Application.Features.Auth.Login;
using EGovServices.Application.Features.Auth.Register;
using EGovServices.Application.Features.Auth.VerifyOtp;
using EGovServices.Application.Features.Profile.Commands.UpdatePhoneNumber;
using EGovServices.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGovServices.API.Controllers;

/// <summary>
/// Authentication controller — IAM System
///
/// Registration Flow (2 steps):
///   Step 1: POST /api/auth/register   → validates citizen, sends OTP
///   Step 2: POST /api/auth/verify-otp → checks OTP, creates User + Wallet
///
/// Login (1 step):
///   POST /api/auth/login → NationalNumber + Password → JWT
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IMediator mediator, AppDbContext context)
    : ControllerBase
{
    // ── STEP 1: Register ─────────────────────────────────────────────

    /// <summary>
    /// Start registration: validates citizen identity, sends OTP to email.
    /// User account is NOT created yet — only after OTP verification.
    ///
    /// POST /api/auth/register
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await mediator.Send(command);

        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    // ── STEP 2: Verify OTP ───────────────────────────────────────────

    /// <summary>
    /// Complete registration: verify OTP, create User + Wallet, return JWT.
    ///
    /// POST /api/auth/verify-otp
    /// </summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpCommand command)
    {
        var result = await mediator.Send(command);

        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    // ── LOGIN ────────────────────────────────────────────────────────

    /// <summary>
    /// Login with NationalNumber and Password.
    /// No OTP needed — direct JWT issuance.
    ///
    /// POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await mediator.Send(command);

        return result.Match < IActionResult > (
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => Unauthorized(new { success = false, message = error }));
    }

    // ── RESEND OTP ───────────────────────────────────────────────────

    /// <summary>
    /// Resend OTP if expired or not received.
    /// Triggers a new RegisterCommand with the same data.
    ///
    /// POST /api/auth/resend-otp
    /// </summary>
    [HttpPost("resend-otp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendOtp([FromBody] RegisterCommand command)
    {
        // Re-uses the same RegisterCommand handler
        // It automatically invalidates old OTPs before creating a new one
        var result = await mediator.Send(command);

        return result.Match< IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    // ── ME ───────────────────────────────────────────────────────────

    /// <summary>
    /// Get current authenticated user info.
    ///
    /// GET /api/auth/me
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Citizen)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return NotFound(new { success = false, message = "المستخدم غير موجود" });

        return Ok(new
        {
            success = true,
            data = new
            {
                user.Id,
                user.NationalNumber,
                user.PhoneNumber,
                user.Email,
                user.Role,
                user.IsVerified,
                user.CreatedAt,

                // فصل البيانات الشخصية بشكل دقيق وآمن
                firstName = user.Citizen?.FirstName,
                lastName = user.Citizen?.LastName,
                fatherName = user.Citizen?.FatherName,
                motherName = user.Citizen?.MotherName,

                // تحويل التاريخ إلى نص لتجنب أي مشاكل توافقية مع خادم النشر أو Swagger
                birthDate = user.Citizen?.BirthDate.ToString("yyyy-MM-dd")
            }
        });


    }

    [HttpPatch("profile/phone")]
    [Authorize]
    public async Task<IActionResult> UpdatePhone([FromBody] UpdatePhoneRequest request)
    {
        // UserId يُستخرج من الـ JWT — المستخدم لا يرسله ولا يقدر يغير رقم غيره
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await mediator.Send(new UpdatePhoneNumberCommand
        {
            UserId = userId,
            NewPhone = request.NewPhone
        });

        return result.IsSuccess
            ? Ok(new { success = true, message = "تم تحديث رقم الجوال بنجاح" })
            : BadRequest(new { success = false, message = result.Error });
    }
}

public sealed record UpdatePhoneRequest(string NewPhone);


