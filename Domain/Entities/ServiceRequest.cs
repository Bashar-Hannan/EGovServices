namespace EGovServices.Domain.Entities;

public class ServiceRequest
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required Guid GovernmentServiceId { get; set; }

    public required string ReferenceNumber { get; set; }
    public required string Status { get; set; }          // يستخدم قيم كلاس RequestStatus أدناه
    public required string FormData { get; set; }        // JSON
    public required DateTime SubmissionDate { get; set; }

    public string? ProcessingNotes { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? CompletedAt { get; set; }

    // ── حقول المواعيد والفروع (القديمة والجديدة معاً) ───────────────────

    // ✅ جديد — ربط بمعرف الموعد الحديث
    public Guid? AppointmentSlotId { get; set; }

    // 🛠️ تم إعادتها — لأن هناك أجزاء في المشروع تقرأ فرع المعاملة مباشرة
    public Guid? BranchId { get; set; }


    // ── حقول الحذف المنطقي ──────────────────────────────────────────
    // ✅ جديد — الحذف المنطقي
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }


    // ── علاقات التنقل (Navigation Properties) ──────────────────────────

    public User User { get; set; } = null!;
    public GovernmentService GovernmentService { get; set; } = null!;

    // ✅ جديد — علاقة الموعد الحديثة
    public AppointmentSlot? AppointmentSlot { get; set; }

    // 🛠️ تم إعادتها — لإصلاح خطأ 'Branch' المفقود في الـ Includes والـ Queries
    public Branch? Branch { get; set; }

    // 🛠️ تم إعادتها — لإصلاح خطأ 'Appointment' القديم إذا كان كود المعاملات لم يُحدث بعد
    public Appointment? Appointment { get; set; }

    // 🛠️ تم إعادتها — لإصلاح أخطاء 'WalletTransactions' المفقودة عند دفع رسوم الخدمة
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = [];

    public ICollection<RequestAuditLog> AuditLogs { get; set; } = [];
    public ICollection<Attachment> Attachments { get; set; } = [];
}

/// <summary>
/// كل الحالات الممكنة للطلب — رقمي وموعد
/// </summary>
public static class RequestStatus
{
    // ── مشتركة ──────────────────────────────────────────
    public const string PendingPayment = "PendingPayment";
    public const string Completed = "Completed";
    public const string Rejected = "Rejected";

    // ── خدمات رقمية فقط ─────────────────────────────────
    public const string Processing = "Processing";

    // ── خدمات الموعد فقط ────────────────────────────────
    public const string DocumentsUnderReview = "DocumentsUnderReview";
    public const string AppointmentConfirmed = "AppointmentConfirmed";
    public const string AppointmentCancelled = "AppointmentCancelled";
}