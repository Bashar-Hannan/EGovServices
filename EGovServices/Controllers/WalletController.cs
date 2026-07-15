using EGovServices.Application.Features.Wallets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public sealed class WalletController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// جلب بيانات المحفظة كاملة — الرصيد + كل المعاملات.
    ///
    /// GET /api/wallet
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWallet(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await mediator.Send(new GetWalletQuery(userId), cancellationToken);

        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }

    /// <summary>
    /// شحن رصيد وهمي للمحفظة — للديمو/الاختبار فقط.
    ///
    /// POST /api/wallet/topup
    /// { "amount": 1000 }
    /// </summary>
    [HttpPost("topup")]
    public async Task<IActionResult> TopUp(
        [FromBody] TopUpRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await mediator.Send(
            new TopUpWalletCommand(userId, request.Amount), cancellationToken);

        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }
}

public sealed record TopUpRequest(decimal Amount);
