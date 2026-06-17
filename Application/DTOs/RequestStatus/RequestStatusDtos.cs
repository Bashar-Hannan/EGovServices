namespace EGovServices.Application.DTOs.RequestStatus;

// sealed record — مطابق لنمط المشروع (FormSchemaResponse, SubmitServiceResponse...)
public sealed record RequestStatusDto(
    string ReferenceNumber,
    string ServiceName,
    string CurrentStatus,
    string CurrentStatusArabic,
    DateTime SubmissionDate,
    List<StatusHistoryItemDto> History
);

public sealed record StatusHistoryItemDto(
    string? OldStatus,       // null في أول سجل (التقديم)
    string NewStatus,
    string? Notes,
    DateTime UpdatedAt
);
