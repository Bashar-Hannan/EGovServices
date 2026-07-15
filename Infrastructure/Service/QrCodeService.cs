using EGovServices.Application.Common.Interfaces;
using QRCoder;

namespace EGovServices.Infrastructure.Service;

/// <summary>
/// يولّد QR Code كـ byte[] جاهز للحقن في QuestPDF.
///
/// NuGet: dotnet add package QRCoder
/// </summary>
public sealed class QrCodeService : IQrCodeService
{
    /// <summary>
    /// يأخذ الـ URL الكامل ويرجع PNG bytes جاهزة للـ PDF.
    /// pixelsPerModule: حجم كل خلية في الـ QR (20 = جودة عالية للـ PDF)
    /// </summary>
    public byte[] GenerateQrCodeBytes(string url, int pixelsPerModule = 20)
    {
        using var generator = new QRCodeGenerator();
        using var data      = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var qrCode    = new PngByteQRCode(data);

        return qrCode.GetGraphic(pixelsPerModule);
    }
}
