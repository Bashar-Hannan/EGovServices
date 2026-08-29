using EGovServices.Application.DTOs;
using EGovServices.Application.DTOs.CivilRecord;
using EGovServices.Application.DTOs.MedicalFile;

namespace EGovServices.Application.Common.Interfaces;

/// <summary>
/// Contract áÊæáíÏ ãáİÇÊ PDF.
/// Application Layer íÚÊãÏ Úáì åĞÇ ÇáÜ Interface İŞØ.
/// ÇáãßÊÈÇÊ ÇáİÚáíÉ (QuestPDF / PuppeteerSharp) ãæÌæÏÉ İí Infrastructure.
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// íæáøÏ ÔåÇÏÉ ÚÏã ÇáãÍßæãíÉ PDF.
    /// </summary>
    Task<string> GenerateClearanceCertificateAsync(ClearanceCertificatePdfData data);

    /// <summary>
    /// íæáøÏ æËíŞÉ ÅÎÑÇÌ ŞíÏ İÑÏí ãÏäí PDF.
    /// íÓÊÎÏã HTML Template + PuppeteerSharp ááÊÍæíá.
    /// </summary>
    Task<string> GenerateCivilRecordAsync(CivilRecordPdfData data);

    /// <summary>
    /// ÙŠÙˆÙ„Ù‘Ø¯ Ù…Ù„Ù PDF Ù„Ù„Ù…Ù„Ù Ø§Ù„Ø·Ø¨ÙŠ Ø§Ù„Ø¥Ù„ÙƒØªØ±ÙˆÙ†ÙŠ (Ø®Ø¯Ù…Ø© ÙˆØ²Ø§Ø±Ø© Ø§Ù„ØµØ­Ø©).
    /// </summary>
    Task<string> GenerateMedicalFileAsync(MedicalFilePdfData data);
}
