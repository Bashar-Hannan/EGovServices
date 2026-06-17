using EGovServices.Application.Common;
using EGovServices.Application.DTOs;
using MediatR;

namespace EGovServices.Application.Features.ClearanceCertificate;

/// <summary>
/// Command: Process a submitted Criminal Record Clearance request.
///
/// Flow triggered by this command:
/// 1. Load the ServiceRequest from DB
/// 2. Extract citizen NationalNumber from JWT claims
/// 3. Query CriminalRecords table
/// 4. Build result text (clean / list of crimes)
/// 5. Generate PDF via IPdfService
/// 6. Save PDF path in Attachments table
/// 7. Update ServiceRequest → Status = "Completed"
/// 8. Return file path + result message
/// </summary>
public sealed record CreateClearanceCertificateCommand
    : IRequest<Result<CreateClearanceCertificateResponse>>
{
    /// <summary>
    /// The ServiceRequest.Id created when the citizen submitted the form.
    /// This is the request we are now processing.
    /// </summary>
    public required Guid ServiceRequestId { get; init; }
}
