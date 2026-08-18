namespace EGovServices.Domain.Entities;

/// <summary>
/// ملف مرفوع من المواطن (صورة، PDF، مستند).
/// مستقل عن ServiceRequest — يُرفع أثناء تعبئة النموذج
/// ثم يُرسَل Guid الخاص به في formData عند Submit.
/// </summary>
public class UploadedFile
{
    public required Guid     Id               { get; set; }
    public required string   FileName         { get; set; }   // الاسم المولَّد على السيرفر
    public required string   OriginalFileName { get; set; }   // الاسم الأصلي من جهاز المواطن
    public required string   FilePath         { get; set; }   // المسار الكامل على القرص
    public required string   ContentType      { get; set; }   // "image/jpeg" | "application/pdf"
    public required long     FileSizeBytes    { get; set; }
    public required Guid     UploadedByUserId { get; set; }   // FK → Users
    public required DateTime CreatedAt        { get; set; }

    // Navigation
    public User UploadedByUser { get; set; } = null!;
}
