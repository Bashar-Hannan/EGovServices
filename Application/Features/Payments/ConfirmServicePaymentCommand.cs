using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Payments;

// ── Command ───────────────────────────────────────────────────────────────────
/// <summary>
/// تأكيد دفع طلب خدمة معيّن من المحفظة الإلكترونية.
/// PaymentProcessingBehavior يلتقطه تلقائياً بسبب IRequiresPaymentCommand
/// ويتولى كل منطق الخصم — هذا الـ Command فارغ تماماً من المنطق.
/// </summary>
public sealed record ConfirmServicePaymentCommand(Guid ServiceRequestId)
    : IRequest<Result<ConfirmPaymentResponse>>, IRequiresPaymentCommand;

// ── DTO ───────────────────────────────────────────────────────────────────────
public sealed record ConfirmPaymentResponse(
    string  ReferenceNumber,
    decimal AmountPaid,
    decimal WalletBalanceAfter,
    string  NewStatus,
    string  Message
);

// ── Handler ───────────────────────────────────────────────────────────────────
/// <summary>
/// الـ Handler هنا "فاضي" عمداً — كل منطق الخصم والتحقق يحدث
/// داخل PaymentProcessingBehavior **قبل** الوصول لهذا الكود.
/// الـ Handler فقط يبني رد النجاح النهائي بعد أن يكون الخصم تم بنجاح.
/// </summary>
public sealed class ConfirmServicePaymentHandler(IAppDbContext context)
    : IRequestHandler<ConfirmServicePaymentCommand, Result<ConfirmPaymentResponse>>
{
    public async Task<Result<ConfirmPaymentResponse>> Handle(
        ConfirmServicePaymentCommand request,
        CancellationToken cancellationToken)
    {
        // في هذه اللحظة، PaymentProcessingBehavior خصم المبلغ مسبقاً
        // ويكفي هنا فقط قراءة النتيجة النهائية من DB لبناء الرد
        var serviceRequest = await context.ServiceRequests
            .AsNoTracking()
            .Include(r => r.GovernmentService)
            .FirstOrDefaultAsync(r => r.Id == request.ServiceRequestId, cancellationToken);

        if (serviceRequest is null)
            return Result<ConfirmPaymentResponse>.Failure("الطلب غير موجود");

        var wallet = await context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == serviceRequest.UserId, cancellationToken);

        return Result<ConfirmPaymentResponse>.Success(new ConfirmPaymentResponse(
            ReferenceNumber:    serviceRequest.ReferenceNumber,
            AmountPaid:         serviceRequest.GovernmentService.ServiceFee,
            WalletBalanceAfter: wallet?.Balance ?? 0,
            NewStatus:          serviceRequest.Status,
            Message:            "تم تأكيد الدفع بنجاح"
        ));
    }
}
