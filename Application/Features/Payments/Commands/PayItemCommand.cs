using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.Payments;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGovServices.Application.Features.Payments.Commands;

// ── Command ───────────────────────────────────────────────────────────────────
public sealed record PayItemCommand(
    string Type,   // "violation" | "electricity"
    Guid ItemId  // Id المخالفة أو الفاتورة
) : IRequest<Result<PayItemResponse>>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class PayItemHandler(
    IAppDbContext context,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<PayItemCommand, Result<PayItemResponse>>
{
    public async Task<Result<PayItemResponse>> Handle(
        PayItemCommand request,
        CancellationToken cancellationToken)
    {
        // ── استخراج هوية المستخدم من JWT ──────────────────────────────
        var userIdClaim = httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var nationalNumber = httpContextAccessor.HttpContext?.User
            .FindFirst("NationalNumber")?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Result<PayItemResponse>.Failure("تعذّر التحقق من هوية المستخدم");

        if (string.IsNullOrEmpty(nationalNumber))
            return Result<PayItemResponse>.Failure("تعذّر التحقق من رقم الهوية");

        return request.Type.ToLower() switch
        {
            "violation" => await PayViolation(request.ItemId, userId, nationalNumber, cancellationToken),
            "electricity" => await PayElectricityBill(request.ItemId, userId, cancellationToken),
            _ => Result<PayItemResponse>.Failure(
                               $"نوع الدفع '{request.Type}' غير مدعوم")
        };
    }

    // ── دفع مخالفة مرورية — مقيَّد بمركبات مواطن الجلسة الحالية ────────
    private async Task<Result<PayItemResponse>> PayViolation(
        Guid violationId, Guid userId, string nationalNumber,
        CancellationToken ct)
    {
        var violation = await context.TrafficViolations
            .FirstOrDefaultAsync(v => v.Id == violationId, ct);

        if (violation is null)
            return Result<PayItemResponse>.Failure("المخالفة غير موجودة");

        if (violation.Status == "Paid")
            return Result<PayItemResponse>.Failure("هذه المخالفة مدفوعة مسبقاً");

        // تحقق ملكية إلزامي — المخالفة لازم تخص مركبة مسجلة باسم المستخدم
        if (violation.CitizenNationalNumber != nationalNumber)
            return Result<PayItemResponse>.Failure("غير مصرح لك بدفع هذه المخالفة");

        var wallet = await GetAndValidateWallet(userId, violation.Amount, ct);
        if (wallet is null)
            return Result<PayItemResponse>.Failure("رصيد المحفظة غير كافٍ أو المحفظة غير موجودة");

        var now = DateTime.UtcNow;
        wallet.Balance -= violation.Amount;

        var transaction = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Amount = violation.Amount,
            TransactionType = "ViolationPayment",
            Description = $"دفع مخالفة مرورية — {violation.ViolationType} — رقم: {violation.ViolationNumber}",
            ReferenceId = violation.ViolationNumber,
            ServiceRequestId = null,
            CreatedAt = now
        };

        await context.WalletTransactions.AddAsync(transaction, ct);

        violation.Status = "Paid";
        violation.PaidAt = now;
        violation.WalletTransactionId = transaction.Id;

        await context.Notifications.AddAsync(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "تمت تسوية مخالفة مرورية",
            Message = $"تم خصم {violation.Amount:F2} ليرة سورية لقاء تسوية المخالفة رقم {violation.ViolationNumber}. الرصيد المتبقي: {wallet.Balance:F2} ليرة سورية.",
            NotificationType = "Success",
            IsRead = false,
            CreatedAt = now
        }, ct);

        await context.SaveChangesAsync(ct);

        return Result<PayItemResponse>.Success(new PayItemResponse(
            Type: "violation",
            ReferenceNumber: violation.ViolationNumber,
            AmountPaid: violation.Amount,
            WalletBalanceAfter: wallet.Balance,
            Message: $"تمت تسوية المخالفة بنجاح. تم خصم {violation.Amount:F2} ليرة سورية."
        ));
    }

    // ── دفع فاتورة كهرباء — غير مقيَّد عمداً (دفع فاتورة أي عداد) ──────
    private async Task<Result<PayItemResponse>> PayElectricityBill(
        Guid billId, Guid userId,
        CancellationToken ct)
    {
        var bill = await context.ElectricityBills
            .FirstOrDefaultAsync(b => b.Id == billId, ct);

        if (bill is null)
            return Result<PayItemResponse>.Failure("الفاتورة غير موجودة");

        if (bill.Status == "Paid")
            return Result<PayItemResponse>.Failure("هذه الفاتورة مدفوعة مسبقاً");

        // ← لا يوجد تحقق ملكية هنا عمداً — يمكن لأي مستخدم دفع فاتورة
        //   أي عداد (مثل دفع فاتورة عن أحد الأقارب)

        var wallet = await GetAndValidateWallet(userId, bill.Amount, ct);
        if (wallet is null)
            return Result<PayItemResponse>.Failure(
                $"رصيد المحفظة غير كافٍ. المبلغ المطلوب: {bill.Amount:F2} ليرة سورية");

        var now = DateTime.UtcNow;
        wallet.Balance -= bill.Amount;

        var transaction = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Amount = bill.Amount,
            TransactionType = "ElectricityPayment",
            Description = $"دفع فاتورة كهرباء — {bill.Month} — رقم: {bill.BillNumber}",
            ReferenceId = bill.BillNumber,
            ServiceRequestId = null,
            CreatedAt = now
        };

        await context.WalletTransactions.AddAsync(transaction, ct);

        bill.Status = "Paid";
        bill.PaidAt = now;
        bill.WalletTransactionId = transaction.Id;

        await context.Notifications.AddAsync(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "تم دفع فاتورة الكهرباء",
            Message = $"تم خصم {bill.Amount:F2} ليرة سورية لقاء فاتورة كهرباء {bill.Month}. الرصيد المتبقي: {wallet.Balance:F2} ليرة سورية.",
            NotificationType = "Success",
            IsRead = false,
            CreatedAt = now
        }, ct);

        await context.SaveChangesAsync(ct);

        return Result<PayItemResponse>.Success(new PayItemResponse(
            Type: "electricity",
            ReferenceNumber: bill.BillNumber,
            AmountPaid: bill.Amount,
            WalletBalanceAfter: wallet.Balance,
            Message: $"تم دفع فاتورة الكهرباء بنجاح. تم خصم {bill.Amount:F2} ليرة سورية."
        ));
    }

    // ── Helper مشترك ─────────────────────────────────────────────────
    private async Task<Wallet?> GetAndValidateWallet(
        Guid userId, decimal amount, CancellationToken ct)
    {
        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        if (wallet is null || wallet.Balance < amount)
            return null;

        return wallet;
    }
}
