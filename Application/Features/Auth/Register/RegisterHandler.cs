using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EGovServices.Application.Features.Auth.Register;

// ── Command (unchanged) ──────────────────────────────────────────

public sealed record RegisterCommand : IRequest<Result<RegisterResponse>>
{
    public required string NationalNumber { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public sealed record RegisterResponse
{
    public required string Message { get; init; }
    public required string NationalNumber { get; init; }
    public required DateTime OtpExpiresAt { get; init; }
}

// ── Handler ──────────────────────────────────────────────────────

public sealed class RegisterHandler(
    IAppDbContext context,
    IEmailService emailService)
    : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Identity Check ─────────────────────────────────────────
        var citizen = await context.Citizens
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.NationalNumber == request.NationalNumber, cancellationToken);

        if (citizen is null)
            return Result<RegisterResponse>.Failure(
                "رقم الهوية الوطنية غير موجود في سجلات الدولة");

        // ── 1.b. Email Match Check — البريد يجب أن يطابق السجلات المدنية ──
        // يمنع أي شخص من تسجيل حساب باسم مواطن آخر ببريد إلكتروني مختلف
        // عن البريد المسجَّل أصلاً في Citizens (المُدخَل من قِبل الأدمن)
        if (!string.Equals(citizen.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            return Result<RegisterResponse>.Failure(
                "البريد الإلكتروني المدخل لا يطابق البريد المسجَّل في السجلات المدنية. " +
                "يرجى استخدام البريد المسجَّل لدى الجهات الحكومية، أو التواصل مع الدعم لتحديثه");

        // ── 2. Duplicate Check ────────────────────────────────────────
        var alreadyRegistered = await context.Users
            .AnyAsync(u => u.NationalNumber == request.NationalNumber, cancellationToken);

        if (alreadyRegistered)
            return Result<RegisterResponse>.Failure(
                "هذا الحساب مفعّل مسبقاً. يرجى تسجيل الدخول");

        // ── 3. Email Duplicate Check ──────────────────────────────────
        var emailTaken = await context.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailTaken)
            return Result<RegisterResponse>.Failure(
                "هذا البريد الإلكتروني مستخدم لحساب آخر");

        // ── 4. Invalidate previous OTPs for this citizen ──────────────
        var oldOtps = await context.OtpVerifications
            .Where(o => o.NationalNumber == request.NationalNumber && !o.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var old in oldOtps)
            old.IsUsed = true;

        // ── 5. Hash password NOW — before saving ──────────────────────
        var passwordHash = HashPassword(request.Password);

        // ── 6. Generate OTP ───────────────────────────────────────────
        var otpCode = GenerateOtp();
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        // ── 7. Save OTP + temp registration data in one record ────────
        var otp = new OtpVerification
        {
            Id = Guid.NewGuid(),
            NationalNumber = request.NationalNumber,
            OtpCode = otpCode,
            ExpiresAt = expiresAt,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            Attempts = 0,

            TempPhoneNumber = request.PhoneNumber,
            TempEmail = request.Email,
            TempPasswordHash = passwordHash
        };

        await context.OtpVerifications.AddAsync(otp, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // ── 8. Send OTP via email ─────────────────────────────────────
        var fullName = $"{citizen.FirstName} {citizen.LastName}";
        await emailService.SendOtpEmailAsync(request.Email, fullName, otpCode);

        return Result<RegisterResponse>.Success(new RegisterResponse
        {
            NationalNumber = request.NationalNumber,
            OtpExpiresAt = expiresAt,
            Message = $"تم إرسال رمز التحقق إلى {MaskEmail(request.Email)}"
        });
    }

    private static string GenerateOtp() =>
        Random.Shared.Next(100000, 999999).ToString();

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return "***";
        var local = parts[0];
        var masked = local.Length <= 2 ? "***" : local[..2] + new string('*', local.Length - 2);
        return $"{masked}@{parts[1]}";
    }
}
