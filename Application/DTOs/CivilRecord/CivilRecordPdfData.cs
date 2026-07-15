namespace EGovServices.Application.DTOs.CivilRecord;

/// <summary>
/// Updated CivilRecordPdfData — أضفنا VerificationToken.
/// </summary>
public sealed record CivilRecordPdfData
{
    public required string NationalNumber  { get; init; }
    public required string FirstName       { get; init; }
    public required string FatherName      { get; init; }
    public required string LastName        { get; init; }
    public required string DateOfBirth     { get; init; }
    public required string PlaceOfBirth    { get; init; }
    public required string MaritalStatus   { get; init; }
    public required string ReferenceNumber { get; init; }
    public required string IssueDate       { get; init; }
    public required string PrintDate       { get; init; }
    public required string DocumentSerial  { get; init; }

    public string MotherFullName { get; init; } = "—";
    public string Religion       { get; init; } = "—";
    public string Gender         { get; init; } = "—";
    public string RecordPlace    { get; init; } = "—";
    public string RecordNumber   { get; init; } = "—";
    public string Remarks        { get; init; } = "";

    // ── NEW ──────────────────────────────────────────────────────────
    public  string VerificationToken { get; init; }
}

public sealed record CreateCivilRecordResponse
{
    public required Guid ServiceRequestId { get; init; }
    public required string ReferenceNumber { get; init; }
    public required string Status { get; init; }
    public required string PdfFilePath { get; init; }
    public required string ResultMessage { get; init; }
}
