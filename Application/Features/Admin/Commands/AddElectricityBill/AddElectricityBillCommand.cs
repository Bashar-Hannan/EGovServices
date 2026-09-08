using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Commands.AddElectricityBill;

// ── Command ───────────────────────────────────────────────────────────────────
public sealed record AddElectricityBillCommand : IRequest<Result<AddElectricityBillResponse>>
{
    public required string CitizenNationalNumber { get; init; }
    public required string MeterNumber { get; init; }
    public required string Month { get; init; }       // مثال: "يونيو 2026"
    public required decimal Amount { get; init; }
}

// ── DTO ───────────────────────────────────────────────────────────────────────
public sealed record AddElectricityBillResponse(
    Guid Id,
    string BillNumber,
    string Message
);

// ── Validator ─────────────────────────────────────────────────────────────────
public sealed class AddElectricityBillValidator : AbstractValidator<AddElectricityBillCommand>
{
    public AddElectricityBillValidator()
    {
        RuleFor(x => x.CitizenNationalNumber)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("رقم الهوية مطلوب (بحد أقصى 20 محرف — نفس عمود NationalNumber بجدول Citizens)");

        RuleFor(x => x.MeterNumber)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("رقم العداد مطلوب");

        RuleFor(x => x.Month)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("الشهر مطلوب");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("قيمة الفاتورة يجب أن تكون أكبر من صفر");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class AddElectricityBillHandler(IAppDbContext context)
    : IRequestHandler<AddElectricityBillCommand, Result<AddElectricityBillResponse>>
{
    public async Task<Result<AddElectricityBillResponse>> Handle(
        AddElectricityBillCommand request, CancellationToken cancellationToken)
    {
        // ── التحقق أن المواطن موجود فعلاً بالسجلات المدنية ─────────────
        var citizenExists = await context.Citizens
            .AnyAsync(c => c.NationalNumber == request.CitizenNationalNumber, cancellationToken);

        if (!citizenExists)
            return Result<AddElectricityBillResponse>.Failure(
                $"لا يوجد مواطن مسجَّل برقم الهوية {request.CitizenNationalNumber}");

        var billNumber = GenerateBillNumber();

        var bill = new ElectricityBill
        {
            Id = Guid.NewGuid(),
            BillNumber = billNumber,
            MeterNumber = request.MeterNumber,
            CitizenNationalNumber = request.CitizenNationalNumber,
            Month = request.Month,
            Amount = request.Amount,
            Status = "Unpaid"
        };

        await context.ElectricityBills.AddAsync(bill, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<AddElectricityBillResponse>.Success(new AddElectricityBillResponse(
            Id: bill.Id,
            BillNumber: billNumber,
            Message: $"تمت إضافة فاتورة الكهرباء {billNumber} بنجاح"
        ));
    }

    // نفس أسلوب GenerateReferenceNumber المستخدم بـ SubmitServiceHandler:
    // طابع زمني + جزء عشوائي قصير لضمان عدم التكرار.
    private static string GenerateBillNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmm");
        var uniquePart = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"ELEC-{timestamp}-{uniquePart}";
    }
}
