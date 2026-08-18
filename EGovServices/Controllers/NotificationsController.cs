using System.Security.Claims;
using EGovServices.Application.Features.Notifications.Commands.MarkNotificationRead;
using EGovServices.Application.Features.Notifications.Queries.GetMyNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    // GET /api/notifications?unreadOnly=true
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] bool unreadOnly = false)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await mediator.Send(new GetMyNotificationsQuery(userId, unreadOnly));

        return result.Match<IActionResult>(
            onSuccess: data  => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    // PATCH /api/notifications/{id}/read
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await mediator.Send(new MarkNotificationReadCommand(id, userId));

        return result.Match<IActionResult>(
            onSuccess: ()     => Ok(new { success = true }),
            onFailure: error  => BadRequest(new { success = false, message = error }));
    }
}
