using EGovServices.Application.DTOs;

namespace EGovServices.Application.Common.Interfaces;

/// <summary>
/// Contract for generating PDF documents.
/// Application layer depends on this interface only.
/// The actual PDF library (QuestPDF) lives in Infrastructure.
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Generates a Criminal Record Clearance Certificate PDF.
    /// </summary>
    /// <param name="data">All data needed to populate the PDF</param>
    /// <returns>
    /// Full file path where the PDF was saved.
    /// Example: "C:\EGovFiles\Certificates\cert_2026_000001.pdf"
    /// </returns>
    Task<string> GenerateClearanceCertificateAsync(ClearanceCertificatePdfData data);
}
