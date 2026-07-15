using EGovServices.Application.Features.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// تأكيد دفع رسوم طلب خدمة من المحفظة الإلكترونية.
    /// كل منطق الخصم يحدث تلقائياً داخل PaymentProcessingBehavior.
    ///
    /// POST /api/payments/confirm/{requestId}
    /// </summary>
    [HttpPost("confirm/{requestId:guid}")]
    public async Task<IActionResult> Confirm(Guid requestId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator.Send(new ConfirmServicePaymentCommand(requestId), cancellationToken);

            return result.Match(
                onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
                onFailure: error => BadRequest(new { success = false, message = error }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
