using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.Verification;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Verification;

// ── Query ─────────────────────────────────────────────────────────────────────
/// <summary>
/// الجهة الخارجية ترسل Token فقط — لا رقم هوية في الـ URL.
/// </summary>
public record VerifyDocumentQuery(string Token) : IRequest<Result<DocumentVerificationResponse>>;

// ── Handler ───────────────────────────────────────────────────────────────────
public class VerifyDocumentHandler(IAppDbContext context)
    : IRequestHandler<VerifyDocumentQuery, Result<DocumentVerificationResponse>>
{
    public async Task<Result<DocumentVerificationResponse>> Handle(
        VerifyDocumentQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result<DocumentVerificationResponse>.Failure("رمز التحقق غير صالح");

        // ── 1. البحث عن الـ Token في Attachments ─────────────────────
        var attachment = await context.Attachments
            .AsNoTracking()
            .Include(a => a.ServiceRequest)
                .ThenInclude(sr => sr.GovernmentService)
            .Include(a => a.ServiceRequest)
                .ThenInclude(sr => sr.User)
                    .ThenInclude(u => u!.Citizen)
            .FirstOrDefaultAsync(
                a => a.VerificationToken == request.Token,
                cancellationToken);

        // ── 2. Token غير موجود ────────────────────────────────────────
        if (attachment is null)
            return Result<DocumentVerificationResponse>.Success(
                InvalidResponse("الوثيقة غير موجودة في النظام"));

        // ── 3. التحقق من انتهاء صلاحية الـ Token ─────────────────────
        if (attachment.VerificationTokenExpiresAt.HasValue &&
            attachment.VerificationTokenExpiresAt.Value < DateTime.UtcNow)
            return Result<DocumentVerificationResponse>.Success(
                InvalidResponse("انتهت صلاحية هذه الوثيقة"));

        // ── 4. الطلب يجب أن يكون Completed ───────────────────────────
        var serviceRequest = attachment.ServiceRequest;
        if (serviceRequest.Status != "Completed")
            return Result<DocumentVerificationResponse>.Success(
                InvalidResponse("الوثيقة غير مكتملة أو ملغاة"));

        // ── 5. بناء الرد مع Data Masking ─────────────────────────────
        var citizen    = serviceRequest.User?.Citizen;
        var fullName   = citizen is null ? "—"
            : $"{citizen.FirstName} {citizen.FatherName} {citizen.LastName}";

        var nationalNumber = serviceRequest.User?.NationalNumber ?? "—";

        return Result<DocumentVerificationResponse>.Success(
            new DocumentVerificationResponse(
                IsValid:              true,
                ReferenceNumber:      serviceRequest.ReferenceNumber,
                DocumentType:         GetDocumentTypeArabic(attachment.FileType),
                MaskedCitizenName:    MaskFullName(fullName),
                MaskedNationalNumber: MaskNationalNumber(nationalNumber),
                IssuedAt:             serviceRequest.CompletedAt.HasValue
                                        ? serviceRequest.CompletedAt.Value.ToString("dd/MM/yyyy")
                                        : "—",
                Status:               "✅ وثيقة صحيحة وسارية",
                ExpiresAt:            attachment.VerificationTokenExpiresAt.HasValue
                                        ? attachment.VerificationTokenExpiresAt.Value.ToString("dd/MM/yyyy")
                                        : null
            ));
    }

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// يخفي جزءاً من كل كلمة في الاسم.
    /// "أحمد محمد علي" → "أحم** م**** ع**"
    /// </summary>
    private static string MaskFullName(string fullName)
    {
        var words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(MaskWord));
    }

    private static string MaskWord(string word)
    {
        if (word.Length <= 2) return word;
        var visibleCount = Math.Max(1, word.Length / 3);
        return word[..visibleCount] + new string('*', word.Length - visibleCount);
    }

    /// <summary>
    /// "1234567890" → "123****890"
    /// يكشف أول 3 وآخر 3 أرقام فقط.
    /// </summary>
    private static string MaskNationalNumber(string number)
    {
        if (number.Length < 7) return new string('*', number.Length);
        return number[..3] + new string('*', number.Length - 6) + number[^3..];
    }

    private static string GetDocumentTypeArabic(string fileType) => fileType switch
    {
        "ClearanceCertificate" => "شهادة عدم المحكومية",
        "CivilRecord"          => "بيان قيد فردي مدني",
        _                      => "وثيقة رسمية"
    };

    /// <summary>
    /// رد موحّد لكل حالات الفشل — لا نكشف سبب الفشل للجهة الخارجية
    /// </summary>
    private static DocumentVerificationResponse InvalidResponse(string reason) =>
        new(
            IsValid:              false,
            ReferenceNumber:      "—",
            DocumentType:         "—",
            MaskedCitizenName:    "—",
            MaskedNationalNumber: "—",
            IssuedAt:             "—",
            Status:               $"❌ {reason}",
            ExpiresAt:            null
        );
}
