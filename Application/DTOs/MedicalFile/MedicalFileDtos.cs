using static System.Net.Mime.MediaTypeNames;

namespace EGovServices.Application.DTOs.MedicalFile;

/// <summary>
/// كل البيانات اللازمة لتوليد PDF الملف الطبي الإلكتروني.
/// بيانات الهوية (FullName/NationalNumber/BirthDate/Gender) تُجلب من
/// Citizen مباشرة — والبيانات الطبية من MedicalRecord — بدون أي
/// إدخال يدوي من المواطن (نفس نمط شهادة عدم المحكومية بالضبط).
/// </summary>
public sealed record MedicalFilePdfData
{
    // ── بيانات المواطن (من Citizen) ──────────────────────────────────
    public required string FullName { get; init; }
    public required string NationalNumber { get; init; }
    public required string BirthDate { get; init; }   // dd/MM/yyyy
    public required string Gender { get; init; }   // "ذكر" | "أنثى" — يُمرَّر كما هو من Citizen.Gender بدون تحويل

    // ── المعلومات الطبية (من MedicalRecord) ──────────────────────────
    public required string BloodType { get; init; }
    public required int HeightCm { get; init; }
    public required int WeightKg { get; init; }
    public required string Allergies { get; init; }
    public required string ChronicDiseases { get; init; }

    // ── بيانات الوثيقة ─────────────────────────────────────────────
    public required string ReferenceNumber { get; init; }
    public required string IssueDate { get; init; } // dd/MM/yyyy
    public required string VerificationToken { get; init; }
}

/// <summary>الرد الذي يُرجعه الـ Handler بعد توليد الوثيقة.</summary>
public sealed record CreateMedicalFileResponse
{
    public required Guid ServiceRequestId { get; init; }
    public required string ReferenceNumber { get; init; }
    public required string Status { get; init; }
    public required string PdfFilePath { get; init; }
    public required string ResultMessage { get; init; }
}
