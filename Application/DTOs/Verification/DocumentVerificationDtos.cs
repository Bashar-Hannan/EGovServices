namespace EGovServices.Application.DTOs.Verification;

/// <summary>
/// رد التحقق من الوثيقة.
/// البيانات مخفية جزئياً (Masked) لحماية الخصوصية.
/// </summary>
public sealed record DocumentVerificationResponse(
    bool    IsValid,
    string  ReferenceNumber,
    string  DocumentType,          // "شهادة عدم المحكومية" / "قيد فردي مدني"
    string  MaskedCitizenName,     // "أحم** م***" بدلاً من "أحمد محمد"
    string  MaskedNationalNumber,  // "123****890" بدلاً من "1234567890"
    string  IssuedAt,              // تاريخ إصدار الوثيقة dd/MM/yyyy
    string  Status,                // "وثيقة صحيحة وسارية" أو "وثيقة غير صالحة"
    string? ExpiresAt              // null = لا تنتهي
);
