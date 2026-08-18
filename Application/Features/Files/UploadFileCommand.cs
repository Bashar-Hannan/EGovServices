using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.FileUpload;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace EGovServices.Application.Features.Files;

// ── Command ───────────────────────────────────────────────────────────────────
public sealed record UploadFileCommand(
    IFormFile File,
    Guid      UploadedByUserId
) : IRequest<Result<UploadFileResponse>>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class UploadFileHandler(
    IAppDbContext        context,
    IFileStorageService  fileStorageService)
    : IRequestHandler<UploadFileCommand, Result<UploadFileResponse>>
{
    // ── الأنواع والحجم المسموحان ──────────────────────────────────────
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/jpg",
        "image/png",
        "application/pdf"
    ];

    private static readonly string[] AllowedExtensions =
    [
        ".jpg", ".jpeg", ".png", ".pdf"
    ];

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;   // 5 MB

    public async Task<Result<UploadFileResponse>> Handle(
        UploadFileCommand request,
        CancellationToken cancellationToken)
    {
        var file = request.File;

        // ── 1. التحقق من وجود الملف ───────────────────────────────────
        if (file is null || file.Length == 0)
            return Result<UploadFileResponse>.Failure("لم يتم اختيار ملف");

        // ── 2. التحقق من الحجم ────────────────────────────────────────
        if (file.Length > MaxFileSizeBytes)
            return Result<UploadFileResponse>.Failure(
                $"حجم الملف يتجاوز الحد المسموح (5 ميجابايت). " +
                $"الحجم الحالي: {file.Length / 1024 / 1024.0:F1} MB");

        // ── 3. التحقق من نوع الملف (ContentType) ─────────────────────
        if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            return Result<UploadFileResponse>.Failure(
                "نوع الملف غير مسموح. الأنواع المقبولة: JPG, PNG, PDF");

        // ── 4. التحقق من الامتداد (أمان إضافي) ───────────────────────
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return Result<UploadFileResponse>.Failure(
                "امتداد الملف غير مسموح. الامتدادات المقبولة: .jpg, .jpeg, .png, .pdf");

        // ── 5. حفظ الملف على القرص ───────────────────────────────────
        string filePath, fileName;
        try
        {
            await using var stream = file.OpenReadStream();
            (filePath, fileName) = await fileStorageService.SaveFileAsync(
                stream,
                file.FileName,
                file.ContentType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<UploadFileResponse>.Failure(
                $"فشل حفظ الملف: {ex.Message}");
        }

        // ── 6. حفظ السجل في قاعدة البيانات ──────────────────────────
        var uploadedFile = new UploadedFile
        {
            Id               = Guid.NewGuid(),
            FileName         = fileName,
            OriginalFileName = file.FileName,
            FilePath         = filePath,
            ContentType      = file.ContentType,
            FileSizeBytes    = file.Length,
            UploadedByUserId = request.UploadedByUserId,
            CreatedAt        = DateTime.UtcNow
        };

        await context.UploadedFiles.AddAsync(uploadedFile, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // ── 7. الرد ───────────────────────────────────────────────────
        return Result<UploadFileResponse>.Success(new UploadFileResponse(
            FileId:           uploadedFile.Id,
            OriginalFileName: file.FileName,
            ContentType:      file.ContentType,
            FileSizeBytes:    file.Length
        ));
    }
}
