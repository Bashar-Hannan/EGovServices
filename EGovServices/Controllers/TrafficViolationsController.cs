using EGovServices.Application.Features.TrafficFines.Commands;
using EGovServices.Application.Features.TrafficFines.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/traffic-violations")]
[Authorize]
public sealed class TrafficViolationsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// استعلام عن المخالفات برقم الهوية أو رقم المركبة أو الاثنين.
    ///
    /// GET /api/traffic-violations?nationalNumber=1234567890
    /// GET /api/traffic-violations?plateNumber=أبج1234
    /// GET /api/traffic-violations?nationalNumber=1234567890&plateNumber=أبج1234
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetViolations(
        [FromQuery] string? nationalNumber,
        [FromQuery] string? plateNumber,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetViolationsQuery(nationalNumber, plateNumber),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error });

        return Ok(new { success = true, data = result.Value });
    }

    /// <summary>
    /// دفع مخالفة مرورية محددة عبر المحفظة الإلكترونية.
    ///
    /// POST /api/traffic-violations/{violationId}/pay
    /// </summary>
    [HttpPost("{violationId:guid}/pay")]
    public async Task<IActionResult> PayViolation(
        Guid violationId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new PayViolationCommand(violationId),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error });

        return Ok(new { success = true, data = result.Value });
    }
}
