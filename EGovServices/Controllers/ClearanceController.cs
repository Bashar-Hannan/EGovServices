using EGovServices.Application.Features.ClearanceCertificate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.API.Controllers;

/// <summary>
/// Handles processing of Criminal Record Clearance Certificate requests.
///
/// Two-step flow:
/// ┌─────────────────────────────────────────────────────────────┐
/// │ Step 1: Citizen submits the service form                    │
/// │   POST /api/services/submit                                 │
/// │   → Creates ServiceRequest with Status = "Pending"         │
/// │   → Returns ReferenceNumber                                 │
/// │                                                             │
/// │ Step 2: Process the request and generate PDF                │
/// │   POST /api/clearance/process/{requestId}                   │
/// │   → Checks CriminalRecords                                  │
/// │   → Generates PDF                                           │
/// │   → Status → "Completed"                                    │
/// │   → Returns PDF file path                                   │
/// └─────────────────────────────────────────────────────────────┘
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require JWT
public sealed class ClearanceController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Process a pending clearance certificate request and generate the PDF.
    ///
    /// POST /api/clearance/process/{requestId}
    ///
    /// Headers:
    ///   Authorization: Bearer {jwt_token}
    ///
    /// Path:
    ///   requestId — the ServiceRequest.Id returned from /api/services/submit
    ///
    /// Returns:
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "serviceRequestId": "...",
    ///     "referenceNumber": "REQ-2026-000001",
    ///     "status": "Completed",
    ///     "pdfFilePath": "C:\\EGovFiles\\Certificates\\cert_REQ_2026_000001_20260510.pdf",
    ///     "resultMessage": "تم إصدار شهادة عدم المحكومية بنجاح"
    ///   }
    /// }
    /// </summary>
    [HttpPost("process/{requestId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Process(Guid requestId)
    {
        var command = new CreateClearanceCertificateCommand
        {
            ServiceRequestId = requestId
        };

        var result = await mediator.Send(command);

        return result.Match<ObjectResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>
    /// Download the generated PDF certificate.
    ///
    /// GET /api/clearance/download/{requestId}
    ///
    /// Returns the PDF file directly as a file download.
    /// The front-end can use this URL to show a "Download" button.
    /// </summary>
    [HttpGet("download/{requestId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid requestId,
        [FromServices] EGovServices.Application.Common.Interfaces.IAppDbContext context)
    {
        // Find the PDF attachment linked to this ServiceRequest
        var attachment = await context.Attachments
            .Where(a => a.ServiceRequestId == requestId
                     && a.FileType == "ClearanceCertificate")
            .OrderByDescending(a => a.Id) // newest if multiple
            .FirstOrDefaultAsync();

        if (attachment is null)
            return NotFound(new
            {
                success = false,
                message = "لم يتم إنشاء الشهادة بعد. قم بتنفيذ /api/clearance/process/{requestId} أولاً"
            });

        if (!System.IO.File.Exists(attachment.FilePath))
            return NotFound(new
            {
                success = false,
                message = "ملف الشهادة غير موجود على السيرفر"
            });

        var fileBytes = await System.IO.File.ReadAllBytesAsync(attachment.FilePath);

        return File(
            fileContents: fileBytes,
            contentType: "application/pdf",
            fileDownloadName: attachment.FileName);
    }
}
