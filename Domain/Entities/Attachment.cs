namespace EGovServices.Domain.Entities;

/// <summary>
/// Updated Attachment — أضفنا VerificationToken للتحقق من صحة الوثائق.
///
/// عند توليد أي وثيقة (شهادة عدم المحكومية، القيد الفردي):
/// - يُولَّد Token عشوائي آمن (32 byte → Base64 URL-safe)
/// - يُخزَّن في هذا الحقل
/// - يُضمَّن في الـ QR Code الموجود في الـ PDF
///
/// مسار التحقق:
/// المستخدم يمسح QR ← يفتح URL مع Token ← API تبحث بالـ Token
/// </summary>
public class Attachment
{
    public required Guid Id { get; set; }
    public required Guid ServiceRequestId { get; set; }
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public required string ContentType { get; set; }
    public required string FileType { get; set; }
    public required long FileSizeBytes { get; set; }

    // ── NEW — Verification Token ─────────────────────────────────────
    /// <summary>
    /// Token آمن يُستخدم للتحقق من صحة الوثيقة.
    /// null = وثيقة قديمة أو مرفق غير قابل للتحقق (صور، إلخ)
    /// </summary>
    public string? VerificationToken { get; set; }

    /// <summary>
    /// تاريخ انتهاء صلاحية الـ Token — null = لا ينتهي
    /// يمكن ضبطه لسنة واحدة مثلاً للشهادات المؤقتة
    /// </summary>
    public DateTime? VerificationTokenExpiresAt { get; set; }

    // Navigation
    public ServiceRequest ServiceRequest { get; set; } = null!;
}
