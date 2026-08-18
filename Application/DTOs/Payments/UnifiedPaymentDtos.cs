namespace EGovServices.Application.DTOs.Payments;

/// <summary>
/// رد الاستعلام — يعمل لكل أنواع الدفع (مخالفات، كهرباء، ...)
/// </summary>
public sealed record PaymentItemDto(
    Guid    Id,
    string  ReferenceNumber,    // VIO-2026-000001 | ELEC-2026-000001
    string  Description,        // "تجاوز السرعة" | "مايو 2026"
    decimal Amount,
    string  Status,             // "Open"/"Paid" | "Unpaid"/"Paid"
    string  StatusLabel,        // "مفتوحة" | "غير مدفوعة"
    string  Date                // تاريخ المخالفة أو الفاتورة
);

public sealed record GetPaymentsResponse(
    string             Type,            // "violation" | "electricity"
    string             TypeLabel,       // "مخالفات مرورية" | "فواتير الكهرباء"
    List<PaymentItemDto> Items,
    int                TotalCount,
    int                UnpaidCount,
    decimal            TotalUnpaidAmount
);

/// <summary>
/// رد الدفع — موحَّد لكل الأنواع
/// </summary>
public sealed record PayItemResponse(
    string  Type,
    string  ReferenceNumber,
    decimal AmountPaid,
    decimal WalletBalanceAfter,
    string  Message
);
