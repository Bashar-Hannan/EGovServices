namespace EGovServices.Application.DTOs;

/// <summary>
/// All data needed to build the PDF certificate.
/// Passed from Handler → IPdfService.
/// </summary>
public sealed record ClearanceCertificatePdfData
{
    public required string FullName { get; init; }
    public required string NationalNumber { get; init; }
    public required string CheckResult { get; init; }
    public required bool HasActiveCrimes { get; init; }
    public required DateOnly IssueDate { get; init; }
    public required string ReferenceNumber { get; init; }
    public string? FormDataJson { get; init; }

    // ── NEW ──────────────────────────────────────────────────────────
    /// <summary>
    /// Token التحقق المولَّد من IVerificationTokenService.
    /// يُحقن في الـ QR Code داخل الـ PDF.
    /// </summary>
    public required string VerificationToken { get; init; }
}

/// <summary>
/// Response returned to the API caller after certificate generation.
/// </summary>
public sealed record CreateClearanceCertificateResponse
{
    public required Guid ServiceRequestId { get; init; }
    public required string ReferenceNumber { get; init; }

    /// <summary>Final status of the request after processing</summary>
    public required string Status { get; init; }

    /// <summary>Path where the PDF was saved on the server</summary>
    public required string PdfFilePath { get; init; }

    /// <summary>Human-readable result message in Arabic</summary>
    public required string ResultMessage { get; init; }
}
