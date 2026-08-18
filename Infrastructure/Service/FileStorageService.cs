using EGovServices.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EGovServices.Infrastructure.Service;

/// <summary>
/// تنفيذ IFileStorageService — يحفظ الملفات على القرص المحلي.
/// مسار التخزين يُقرأ من appsettings.json: "FileStorage:UploadsPath"
/// </summary>
public sealed class FileStorageService(IConfiguration config) : IFileStorageService
{
    private readonly string _uploadsPath =
        config["FileStorage:UploadsPath"] ?? Path.Combine(
            Directory.GetCurrentDirectory(), "Uploads");

    public async Task<(string FilePath, string FileName)> SaveFileAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // ── 1. التأكد من وجود المجلد ─────────────────────────────────
        if (!Directory.Exists(_uploadsPath))
            Directory.CreateDirectory(_uploadsPath);

        // ── 2. توليد اسم فريد (Guid + الامتداد الأصلي) ───────────────
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var fileName  = $"{Guid.NewGuid()}{extension}";
        var fullPath  = Path.Combine(_uploadsPath, fileName);

        // ── 3. حفظ الملف على القرص ───────────────────────────────────
        await using var fileOutput = File.Create(fullPath);
        await fileStream.CopyToAsync(fileOutput, cancellationToken);

        return (fullPath, fileName);
    }
}
