using EGovServices.Domain.Enums;

namespace EGovServices.Domain.Entities;

public class GovernmentService
{
    public required Guid Id { get; set; }

    // ✅ الحفاظ على ربط الخدمة بالجهة الحكومية لتجنب الأخطاء
    public required Guid GovernmentEntityId { get; set; }

    public required string Name { get; set; }
    public required string Description { get; set; }

    // ✅ إعادة حقل الشروط لأن بقية المشروع يعتمد عليه
    public string? Requirements { get; set; }

    // 🛠️ تصحيح الخطأ الإملائي من ServiceFeeFee إلى ServiceFee ليتوافق مع بقية المشروع
    public required decimal ServiceFee { get; set; }

    public required bool IsActive { get; set; } = true;

    // ✨ ميزة جديدة: تاريخ الإنشاء
    public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ✨ ميزة جديدة: نوع الخدمة (رقمية أو حضورية)
    public required ServiceType ServiceType { get; set; }

    // ── العلاقات (Navigation Properties) ─────────────────────────────────

    // الجهة الحكومية التابعة لها الخدمة
    public GovernmentEntity GovernmentEntity { get; set; } = null!;

    // الحقول الديناميكية للنموذج
    public ICollection<ServiceFormField> FormFields { get; set; } = [];

    // 💡 لحل مشكلة 'ServiceRequests': نقوم بتسميتها الإسمين معاً عبر تفعيل الإسم القديم أو الجديد
    // لكن الأفضل الحفاظ على الاسم القديم لكي لا تعدل في 20 ملف آخر:
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = [];

    // 💡 لحل مشكلة 'ServiceSlots': حافظ على الاسم القديم أو اجعل الجديد متوافقاً
    // سنبقي على الاسم القديم لعدم كسر الـ Handlers الحالية:
    public ICollection<ServiceSlot> ServiceSlots { get; set; } = [];

    // إذا كنت مجبراً على استخدام كلاس المواعيد الجديد AppointmentSlot، غير الحقل البرمجي أعلاه لـ:
    // public ICollection<AppointmentSlot> ServiceSlots { get; set; } = [];
}