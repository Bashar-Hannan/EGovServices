using System.Security.Claims;
using EGovServices.Application.Features.Payments.Commands;
using EGovServices.Application.Features.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGovServices.API.Controllers;

/// <summary>
/// نظام الدفع الموحَّد — يعمل لكل أنواع الدفع.
///
/// GET  /api/payments/{type}/{referenceNumber}
/// POST /api/payments/{type}/{itemId}/pay
///
/// Types المدعومة:
///   violation   → مخالفات مرورية (referenceNumber = رقم المركبة)
///                 مقيَّدة بمركبات مواطن الجلسة الحالية فقط
///   electricity → فواتير الكهرباء (referenceNumber = رقم العداد)
///                 غير مقيَّدة (يمكن دفع فاتورة أي عداد)
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// استعلام عن المدفوعات.
    ///
    /// GET /api/payments/violation/أبج1234
    /// GET /api/payments/electricity/MTR-001234
    /// </summary>
    [HttpGet("{type}/{referenceNumber}")]
    public async Task<IActionResult> GetPayments(
        string type,
        string referenceNumber,
        CancellationToken cancellationToken)
    {
        var nationalNumber = User.FindFirst("NationalNumber")?.Value;

        var result = await mediator.Send(
            new GetPaymentsQuery(type, referenceNumber, nationalNumber),
            cancellationToken);

        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>
    /// دفع عنصر محدد من المحفظة.
    ///
    /// POST /api/payments/violation/{violationId}/pay
    /// POST /api/payments/electricity/{billId}/pay
    /// </summary>
    [HttpPost("{type}/{itemId:guid}/pay")]
    public async Task<IActionResult> Pay(
        string type,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new PayItemCommand(type, itemId),
            cancellationToken);

        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }
}
