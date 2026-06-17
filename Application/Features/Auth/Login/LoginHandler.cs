using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EGovServices.Application.Features.Auth.Login;

// ── Command ──────────────────────────────────────────────────────

/// <summary>
/// Login using NationalNumber + Password.
/// No OTP required — OTP is only for first-time registration.
/// </summary>
public sealed record LoginCommand : IRequest<Result<LoginResponse>>
{
    public required string NationalNumber { get; init; }
    public required string Password { get; init; }
}

public sealed record LoginResponse
{
    public required string Token { get; init; }
    public required string Role { get; init; }
    public required Guid UserId { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

// ── Handler ──────────────────────────────────────────────────────

public sealed class LoginHandler(
    IAppDbContext context,
    IJwtService jwtService)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Find user by NationalNumber ────────────────────────────
        var user = await context.Users
            .FirstOrDefaultAsync(
                u => u.NationalNumber == request.NationalNumber,
                cancellationToken);

        // Use same error for both "not found" and "wrong password"
        // WHY: Don't reveal which one failed (security best practice)
        if (user is null)
            return Result<LoginResponse>.Failure(
                "بيانات الدخول غير صحيحة");

        // ── 2. Check IsActive ─────────────────────────────────────────
        if (!user.IsActive)
            return Result<LoginResponse>.Failure(
                "هذا الحساب موقوف. يرجى التواصل مع الدعم");

        // ── 3. Check IsVerified ───────────────────────────────────────
        // New accounts must verify OTP before login
        // Old seed-data accounts have IsVerified = true by default
        if (!user.IsVerified)
            return Result<LoginResponse>.Failure(
                "يرجى تفعيل حسابك أولاً عبر رمز التحقق المرسل لبريدك الإلكتروني");

        // ── 4. Verify password ────────────────────────────────────────
        var passwordHash = HashPassword(request.Password);

        if (user.PasswordHash != passwordHash)
            return Result<LoginResponse>.Failure(
                "بيانات الدخول غير صحيحة");

        // ── 5. Generate JWT ───────────────────────────────────────────
        var token = jwtService.GenerateToken(user);

        return Result<LoginResponse>.Success(new LoginResponse
        {
            Token = token,
            Role = user.Role,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        });
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(
            sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }
}

