using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Auth.VerifyOtp;

// ── Command — only OTP data needed ──────────────────────────────

/// <summary>
/// Step 2: Verify OTP code.
/// Only NationalNumber and OtpCode are required.
/// Phone, Email, and Password were already saved in the OTP record during Step 1.
/// </summary>
public sealed record VerifyOtpCommand : IRequest<Result<VerifyOtpResponse>>
{
    public required string NationalNumber { get; init; }
    public required string OtpCode { get; init; }
}

public sealed record VerifyOtpResponse
{
    public required Guid UserId { get; init; }
    public required string Token { get; init; }
    public required string Message { get; init; }
}

// ── Handler ──────────────────────────────────────────────────────

public sealed class VerifyOtpHandler(
    IAppDbContext context,
    IJwtService jwtService)
    : IRequestHandler<VerifyOtpCommand, Result<VerifyOtpResponse>>
{
    private const int MaxAttempts = 3;

    public async Task<Result<VerifyOtpResponse>> Handle(
        VerifyOtpCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Find the latest unused OTP for this citizen ────────────
        var otp = await context.OtpVerifications
            .Where(o => o.NationalNumber == request.NationalNumber && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
            return Result<VerifyOtpResponse>.Failure(
                "لا يوجد رمز تحقق نشط. يرجى طلب رمز جديد");

        // ── 2. Check expiry ───────────────────────────────────────────
        if (DateTime.UtcNow > otp.ExpiresAt)
        {
            otp.IsUsed = true;
            await context.SaveChangesAsync(cancellationToken);
            return Result<VerifyOtpResponse>.Failure(
                "انتهت صلاحية رمز التحقق. يرجى طلب رمز جديد");
        }

        // ── 3. Check attempt limit ────────────────────────────────────
        if (otp.Attempts >= MaxAttempts)
            return Result<VerifyOtpResponse>.Failure(
                "تم تجاوز الحد الأقصى للمحاولات. يرجى طلب رمز جديد");

        // ── 4. Verify OTP code ────────────────────────────────────────
        if (otp.OtpCode != request.OtpCode)
        {
            otp.Attempts++;
            await context.SaveChangesAsync(cancellationToken);

            var remaining = MaxAttempts - otp.Attempts;
            return Result<VerifyOtpResponse>.Failure(
                remaining > 0
                    ? $"رمز التحقق غير صحيح. المحاولات المتبقية: {remaining}"
                    : "رمز التحقق غير صحيح. تم تجاوز الحد الأقصى");
        }

        // ── 5. Mark OTP as used ───────────────────────────────────────
        otp.IsUsed = true;

        // ── 6. Read registration data saved in Step 1 ─────────────────
        // No need to ask the citizen again — everything is in the OTP record
        var phoneNumber = otp.TempPhoneNumber;
        var email = otp.TempEmail;
        var passwordHash = otp.TempPasswordHash;

        // ── 7. Create User ────────────────────────────────────────────
        var user = new User
        {
            Id = Guid.NewGuid(),
            NationalNumber = request.NationalNumber,
            PhoneNumber = phoneNumber,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsVerified = true,
            Role = "Citizen"
        };

        await context.Users.AddAsync(user, cancellationToken);

        // ── 8. Create Wallet ──────────────────────────────────────────
        await context.Wallets.AddAsync(new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Balance = 0m,
            Currency = "SAR",
            LastUpdated = DateTime.UtcNow,
            IsLocked = false
        }, cancellationToken);

        // ── 9. Save OTP + User + Wallet atomically ────────────────────
        await context.SaveChangesAsync(cancellationToken);

        // ── 10. Generate JWT ──────────────────────────────────────────
        var token = jwtService.GenerateToken(user);

        return Result<VerifyOtpResponse>.Success(new VerifyOtpResponse
        {
            UserId = user.Id,
            Token = token,
            Message = "تم تفعيل حسابك بنجاح! مرحباً بك في منصة الخدمات الحكومية"
        });
    }
}
