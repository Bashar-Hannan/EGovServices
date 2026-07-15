using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using EGovServices.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGovServices.Application.Common.Behaviors;

/// <summary>
/// يلتقط تلقائياً أي Command يُطبّق IRequiresPaymentCommand،
/// يتحقق من الرصيد، يخصم من المحفظة، يسجّل WalletTransaction،
/// ويغيّر حالة الطلب من "PendingPaymentPayment" إلى الحالة التالية.
///
/// ⚠️ يجب تسجيله في Program.cs **قبل** StatusTrackingBehavior
/// حتى يقرأ StatusTrackingBehavior الحالة الجديدة الصحيحة بعد الخصم.
/// </summary>
public class PaymentProcessingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAppDbContext        _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PaymentProcessingBehavior(
        IAppDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context             = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // فقط الـ Commands التي تُطبّق IRequiresPaymentCommand تُعالَج هنا
        if (request is not IRequiresPaymentCommand paymentCommand)
            return await next();

        // ── 1. استخراج هوية المستخدم من JWT ──────────────────────────
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("تعذّر التحقق من هوية المستخدم");

        // ── 2. تحميل الطلب + الخدمة (لمعرفة الرسوم) ──────────────────
        var serviceRequest = await _context.ServiceRequests
            .Include(r => r.GovernmentService)
            .FirstOrDefaultAsync(r => r.Id == paymentCommand.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException("الطلب غير موجود");

        // ── 3. التحقق من ملكية الطلب ──────────────────────────────────
        if (serviceRequest.UserId != userId)
            throw new UnauthorizedAccessException("غير مصرح لك بدفع هذا الطلب");

        // ── 4. التحقق أن الطلب فعلاً في حالة "بانتظار الدفع" ─────────
        if (serviceRequest.Status != "PendingPaymentPayment")
            throw new InvalidOperationException(
                $"لا يمكن الدفع — حالة الطلب الحالية: {serviceRequest.Status}");

        // ── 5. تحميل المحفظة ──────────────────────────────────────────
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("المحفظة غير موجودة");

        var fee = serviceRequest.GovernmentService.ServiceFee;

        // ── 6. التحقق من كفاية الرصيد ─────────────────────────────────
        if (wallet.Balance < fee)
            throw new InvalidOperationException(
                $"رصيد المحفظة غير كافٍ. الرصيد الحالي: {wallet.Balance:F2} ريال، " +
                $"المطلوب: {fee:F2} ريال");

        var now = DateTime.UtcNow;
        var oldStatus = serviceRequest.Status;

        // ── 7. خصم المبلغ ──────────────────────────────────────────────
        wallet.Balance -= fee;

        // ── 8. تسجيل معاملة المحفظة ───────────────────────────────────
        _context.WalletTransactions.Add(new WalletTransaction
        {
            Id              = Guid.NewGuid(),
            WalletId        = wallet.Id,
            Amount          = fee,
            TransactionType = "ServiceFeePayment",
            Description     = $"دفع رسوم خدمة {serviceRequest.GovernmentService.Name} — " +
                               $"طلب رقم {serviceRequest.ReferenceNumber}",
            CreatedAt       = now
        });

        // ── 9. تغيير حالة الطلب — نفس منطق نوع الخدمة المُستخدَم سابقاً ──
        //     خدمات رقمية بالكامل تتحول مباشرة لـ Processing (معالجة فورية تلقائية)
        //     خدمات الحضور تتحول إلى Submitted (تنتظر دور الموظف)
        serviceRequest.Status = serviceRequest.GovernmentService.ServiceType switch
        {
            ServiceType.Digital => "Processing",
            _                   => "Submitted"
        };

        await _context.SaveChangesAsync(cancellationToken);

        // ── 10. تمرير التنفيذ لباقي الـ Pipeline (الـ Handler الفعلي) ──
        //      StatusTrackingBehavior الآتي بعده سيرى أن oldStatus != newStatus
        //      ويسجّل AuditLog + Notification تلقائياً بدون أي كود إضافي هنا
        return await next();
    }
}
