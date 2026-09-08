using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Commands.CancelElectricityBill;

// ─── Command ──────────────────────────────────────────────────────────────────
// إلغاء "ناعم" (Status = "Cancelled") وليس حذفاً فعلياً — نحافظ على
// السجل بدل حذفه، بعكس المخالفة (انظر DeleteTrafficViolationCommand)،
// لأن الفاتورة أقرب لمستند مالي يُفضَّل تتبّع تاريخه بدل محوه.
public sealed record CancelElectricityBillCommand(Guid BillId)
    : IRequest<Result<CancelElectricityBillResponse>>;

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record CancelElectricityBillResponse(
    Guid BillId,
    string BillNumber,
    string Message
);

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class CancelElectricityBillHandler(IAppDbContext context)
    : IRequestHandler<CancelElectricityBillCommand, Result<CancelElectricityBillResponse>>
{
    public async Task<Result<CancelElectricityBillResponse>> Handle(
        CancelElectricityBillCommand request, CancellationToken cancellationToken)
    {
        var bill = await context.ElectricityBills
            .FirstOrDefaultAsync(b => b.Id == request.BillId, cancellationToken);

        if (bill is null)
            return Result<CancelElectricityBillResponse>.Failure("الفاتورة غير موجودة");

        if (bill.Status == "Paid")
            return Result<CancelElectricityBillResponse>.Failure(
                "لا يمكن إلغاء فاتورة مدفوعة مسبقاً — هذه الفاتورة مرتبطة بعملية دفع فعلية");

        if (bill.Status == "Cancelled")
            return Result<CancelElectricityBillResponse>.Failure("هذه الفاتورة ملغاة أصلاً");

        bill.Status = "Cancelled";
        await context.SaveChangesAsync(cancellationToken);

        return Result<CancelElectricityBillResponse>.Success(new CancelElectricityBillResponse(
            BillId: request.BillId,
            BillNumber: bill.BillNumber,
            Message: $"تم إلغاء الفاتورة {bill.BillNumber} بنجاح"
        ));
    }
}
