namespace EGovServices.Application.DTOs.TrafficFines;

/// <summary>بيانات مخالفة واحدة — يُرجعها الاستعلام</summary>
public sealed record ViolationDto(
    Guid    Id,
    string  ViolationNumber,
    string  PlateNumber,
    string  ViolationType,
    string? Description,
    decimal Amount,
    string  Location,
    string  ViolationDate,
    string  Status
);

/// <summary>رد الاستعلام — قائمة المخالفات مع الإجمالي</summary>
public sealed record GetViolationsResponse(
    List<ViolationDto> Violations,
    int     TotalCount,
    int     OpenCount,
    decimal TotalOpenAmount
);

/// <summary>رد الدفع — تأكيد العملية</summary>
public sealed record PayViolationResponse(
    string  ViolationNumber,
    decimal AmountPaid,
    decimal WalletBalanceAfter,
    string  Message
);
