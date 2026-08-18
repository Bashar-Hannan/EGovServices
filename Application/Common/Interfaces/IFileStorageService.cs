namespace EGovServices.Application.Common.Interfaces;

/// <summary>
/// Contract لحفظ الملفات على القرص.
/// Application Layer يعتمد على هذا Interface فقط.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// يحفظ الملف على القرص ويرجع المسار الكامل.
    /// اسم الملف يُولَّد تلقائياً (Guid + الامتداد الأصلي) لمنع التعارض.
    /// </summary>
    Task<(string FilePath, string FileName)> SaveFileAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
