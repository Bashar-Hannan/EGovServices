namespace EGovServices.Application.Common.Interfaces;

/// <summary>
/// Contract لتوليد QR Code bytes.
/// </summary>
public interface IQrCodeService
{
    /// <summary>
    /// يأخذ URL ويرجع PNG bytes جاهزة للحقن في QuestPDF.
    /// </summary>
    byte[] GenerateQrCodeBytes(string url, int pixelsPerModule = 20);
}
