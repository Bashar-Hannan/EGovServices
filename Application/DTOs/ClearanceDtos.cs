namespace EGovServices.Application.DTOs;

/// <summary>
/// All data needed to build the PDF certificate.
/// Passed from Handler → IPdfService.
/// </summary>
public sealed record ClearanceCertificatePdfData
{
    /// <summary>Full name of the citizen (First + Father + Last)</summary>
    public required string FullName { get; init; }

    /// <summary>National ID number printed on the certificate</summary>
    public required string NationalNumber { get; init; }

    /// <summary>
    /// "لا توجد سوابق جنائية" OR list of crimes if any found
    /// </summary>
    public required string CheckResult { get; init; }

    /// <summary>
    /// Whether the citizen has active criminal records.
    /// Determines the certificate stamp color (green/red) in the PDF.
    /// </summary>
    public required bool HasActiveCrimes { get; init; }

    /// <summary>Date the certificate was issued (printed on document)</summary>
    public required DateOnly IssueDate { get; init; }

    /// <summary>Unique certificate reference number (REQ-2026-000001)</summary>
    public required string ReferenceNumber { get; init; }

    /// <summary>
    /// Raw FormData from ServiceRequest (JSON).
    /// Used to print any extra fields the citizen filled in the dynamic form.
    /// Optional — can be null if no extra fields defined for this service.
    /// </summary>
    public string? FormDataJson { get; init; }
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
