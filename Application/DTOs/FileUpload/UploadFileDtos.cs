namespace EGovServices.Application.DTOs.FileUpload;

/// <summary>
/// رد رفع الملف — يُرسَل للفرونت بعد الحفظ.
/// الفرونت يحفظ FileId ويضعه في formData عند Submit.
/// </summary>
public sealed record UploadFileResponse(
    Guid   FileId,           // يُرسَل في formData["fieldName"]
    string OriginalFileName, // للعرض في الواجهة "passport.jpg"
    string ContentType,
    long   FileSizeBytes
);
