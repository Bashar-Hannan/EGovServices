using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Payments.Queries;

// ── Query ─────────────────────────────────────────────────────────────────────
/// <summary>
/// استعلام موحَّد عن أي نوع دفع.
/// type: "violation"   → يستعلم TrafficViolations برقم المركبة — مقيَّد
///                        بمواطن الجلسة الحالية فقط (RequestingNationalNumber)
/// type: "electricity" → يستعلم ElectricityBills برقم العداد — غير مقيَّد
///                        (يمكن دفع فاتورة لأي عداد، مثل عداد أحد الأقارب)
/// </summary>
public sealed record GetPaymentsQuery(
    string Type,                       // "violation" | "electricity"
    string ReferenceNumber,            // رقم المركبة للمخالفات | رقم العداد للكهرباء
    string? RequestingNationalNumber   // من JWT — يُستخدم فقط لتقييد المخالفات
) : IRequest<Result<GetPaymentsResponse>>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetPaymentsHandler(IAppDbContext context)
    : IRequestHandler<GetPaymentsQuery, Result<GetPaymentsResponse>>
{
    public async Task<Result<GetPaymentsResponse>> Handle(
        GetPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReferenceNumber))
            return Result<GetPaymentsResponse>.Failure("يجب إدخال رقم المرجع للاستعلام");

        return request.Type.ToLower() switch
        {
            "violation" => await GetViolations(request.ReferenceNumber, request.RequestingNationalNumber, cancellationToken),
            "electricity" => await GetElectricityBills(request.ReferenceNumber, cancellationToken),
            _ => Result<GetPaymentsResponse>.Failure(
                               $"نوع الدفع '{request.Type}' غير مدعوم. الأنواع المتاحة: violation, electricity")
        };
    }

    // ── المخالفات المرورية — مقيَّدة بمواطن الجلسة الحالية فقط ──────────
    private async Task<Result<GetPaymentsResponse>> GetViolations(
        string plateNumber, string? requestingNationalNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestingNationalNumber))
            return Result<GetPaymentsResponse>.Failure("تعذّر التحقق من هوية المستخدم");

        // ⚠️ تحقق ملكية إلزامي: المركبة يجب أن تكون مسجَّلة باسم صاحب
        // الجلسة الحالية — أي مواطن ما بيقدر يشوف مخالفات مركبة غير مركبته
        var violations = await context.TrafficViolations
            .AsNoTracking()
            .Where(v => v.PlateNumber == plateNumber &&
                        v.CitizenNationalNumber == requestingNationalNumber)
            .OrderBy(v => v.Status == "Open" ? 0 : 1)
            .ThenByDescending(v => v.ViolationDate)
            .ToListAsync(ct);

        if (violations.Count == 0)
            return Result<GetPaymentsResponse>.Failure(
                "لا توجد مخالفات مسجلة على هذه المركبة باسمك");

        var items = violations.Select(v => new PaymentItemDto(
            Id: v.Id,
            ReferenceNumber: v.ViolationNumber,
            Description: v.ViolationType,
            Amount: v.Amount,
            Status: v.Status,
            StatusLabel: v.Status == "Open" ? "مفتوحة" : "مدفوعة",
            Date: v.ViolationDate.ToString("dd/MM/yyyy")
        )).ToList();

        var unpaid = violations.Where(v => v.Status == "Open").ToList();

        return Result<GetPaymentsResponse>.Success(new GetPaymentsResponse(
            Type: "violation",
            TypeLabel: "مخالفات مرورية",
            Items: items,
            TotalCount: violations.Count,
            UnpaidCount: unpaid.Count,
            TotalUnpaidAmount: unpaid.Sum(v => v.Amount)
        ));
    }

    // ── فواتير الكهرباء — غير مقيَّدة عمداً (دفع فاتورة عداد لأي شخص) ──
    private async Task<Result<GetPaymentsResponse>> GetElectricityBills(
        string meterNumber, CancellationToken ct)
    {
        var bills = await context.ElectricityBills
            .AsNoTracking()
            .Where(b => b.MeterNumber == meterNumber)
            .OrderBy(b => b.Status == "Unpaid" ? 0 : 1)
            .ToListAsync(ct);

        if (bills.Count == 0)
            return Result<GetPaymentsResponse>.Failure(
                "لا توجد فواتير مسجلة لهذا العداد");

        var items = bills.Select(b => new PaymentItemDto(
            Id: b.Id,
            ReferenceNumber: b.BillNumber,
            Description: $"فاتورة كهرباء — {b.Month}",
            Amount: b.Amount,
            Status: b.Status,
            StatusLabel: b.Status == "Unpaid" ? "غير مدفوعة" : "مدفوعة",
            Date: b.Month
        )).ToList();

        var unpaid = bills.Where(b => b.Status == "Unpaid").ToList();

        return Result<GetPaymentsResponse>.Success(new GetPaymentsResponse(
            Type: "electricity",
            TypeLabel: "فواتير الكهرباء",
            Items: items,
            TotalCount: bills.Count,
            UnpaidCount: unpaid.Count,
            TotalUnpaidAmount: unpaid.Sum(b => b.Amount)
        ));
    }
}
