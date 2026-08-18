using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.TrafficFines;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGovServices.Application.Features.TrafficFines.Commands;

// ── Command ───────────────────────────────────────────────────────────────────
public record PayViolationCommand(Guid ViolationId)
    : IRequest<Result<PayViolationResponse>>;

// ── Handler ───────────────────────────────────────────────────────────────────
public class PayViolationHandler(
    IAppDbContext context,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<PayViolationCommand, Result<PayViolationResponse>>
{
    public async Task<Result<PayViolationResponse>> Handle(
        PayViolationCommand request,
        CancellationToken cancellationToken)
    {
        // ── STEP 1: استخراج هوية المستخدم من JWT ──────────────────────
        var userIdClaim = httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var nationalNumber = httpContextAccessor.HttpContext?.User
            .FindFirst("NationalNumber")?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Result<PayViolationResponse>.Failure("تعذّر التحقق من هوية المستخدم");

        if (string.IsNullOrEmpty(nationalNumber))
            return Result<PayViolationResponse>.Failure("تعذّر التحقق من رقم الهوية");

        // ── STEP 2: تحميل المخالفة ────────────────────────────────────
        var violation = await context.TrafficViolations
            .FirstOrDefaultAsync(v => v.Id == request.ViolationId, cancellationToken);

        if (violation is null)
            return Result<PayViolationResponse>.Failure("المخالفة غير موجودة");

        if (violation.Status == "Paid")
            return Result<PayViolationResponse>.Failure("هذه المخالفة مدفوعة مسبقاً");

        // ── STEP 3: التحقق من ملكية المخالفة ─────────────────────────
        if (violation.CitizenNationalNumber != nationalNumber)
            return Result<PayViolationResponse>.Failure("غير مصرح لك بدفع هذه المخالفة");

        // ── STEP 4: تحميل المحفظة ─────────────────────────────────────
        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wallet is null)
            return Result<PayViolationResponse>.Failure("المحفظة غير موجودة");

        // ── STEP 5: التحقق من كفاية الرصيد ───────────────────────────
        if (wallet.Balance < violation.Amount)
            return Result<PayViolationResponse>.Failure(
                $"رصيد المحفظة غير كافٍ. " +
                $"الرصيد الحالي: {wallet.Balance:F2} ريال، " +
                $"المبلغ المطلوب: {violation.Amount:F2} ريال");

        var now = DateTime.UtcNow;

        // ── STEP 6: خصم المبلغ من المحفظة ────────────────────────────
        wallet.Balance -= violation.Amount;

        // ── STEP 7: تسجيل معاملة المحفظة ─────────────────────────────
        var walletTransaction = new WalletTransaction
        {
            Id              = Guid.NewGuid(),
            WalletId        = wallet.Id,
            Amount          = violation.Amount,
            TransactionType = "ViolationPayment",
            Description     = $"دفع مخالفة مرورية — {violation.ViolationType} — رقم: {violation.ViolationNumber}",
            ReferenceId     = violation.ViolationNumber,   // ← حقل جديد موجود في Entity
            ServiceRequestId = null,                        // مش مرتبطة بـ ServiceRequest
            CreatedAt       = now
        };

        await context.WalletTransactions.AddAsync(walletTransaction, cancellationToken);

        // ── STEP 8: تحديث حالة المخالفة → Paid ───────────────────────
        violation.Status              = "Paid";
        violation.PaidAt              = now;
        violation.WalletTransactionId = walletTransaction.Id;

        // ── STEP 9: إرسال إشعار للمواطن ──────────────────────────────
        await context.Notifications.AddAsync(new Notification
        {
            Id               = Guid.NewGuid(),
            UserId           = userId,
            Title            = "تمت تسوية مخالفة مرورية",
            Message          = $"تم خصم {violation.Amount:F2} ريال من محفظتك لقاء تسوية " +
                               $"المخالفة رقم {violation.ViolationNumber} " +
                               $"({violation.ViolationType}). " +
                               $"الرصيد المتبقي: {wallet.Balance:F2} ريال.",
            NotificationType = "Success",
            IsRead           = false,
            CreatedAt        = now
        }, cancellationToken);

        // ── STEP 10: حفظ كل شيء دفعة واحدة (Atomic) ─────────────────
        await context.SaveChangesAsync(cancellationToken);

        // ── STEP 11: الرد ─────────────────────────────────────────────
        return Result<PayViolationResponse>.Success(new PayViolationResponse(
            ViolationNumber:    violation.ViolationNumber,
            AmountPaid:         violation.Amount,
            WalletBalanceAfter: wallet.Balance,
            Message:            $"تمت تسوية المخالفة بنجاح. تم خصم {violation.Amount:F2} ريال من محفظتك."
        ));
    }
}
