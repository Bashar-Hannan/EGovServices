using EGovServices.Application.DTOs.ServiceSubmission;
using EGovServices.Application.Features.Services.Commands.SubmitService;
using EGovServices.Application.Features.Services.Queries.GetServiceFormSchema;
using EGovServices.Application.Features.Services.Queries.SearchServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ServicesController(IMediator mediator) : ControllerBase
{
    // GET /api/services/{id}/schema
    [HttpGet("{id:guid}/schema")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFormSchema(Guid id)
    {
        var result = await mediator.Send(new GetServiceFormSchemaQuery(id));

        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }

    // POST /api/services/submit
    [HttpPost("submit")]
    [Authorize]
    public async Task<IActionResult> Submit([FromBody] SubmitServiceRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { success = false, message = "غير مصرح. الرجاء تسجيل الدخول" });

        var result = await mediator.Send(new SubmitServiceCommand
        {
            ServiceId = request.ServiceId,
            UserId = userId,
            FormData = request.FormData
            // BranchId محذوف — يجي جوا FormData
        });

        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    // GET /api/services/search?q=جواز
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { success = false, message = "أدخل كلمة للبحث" });

        var result = await mediator.Send(new SearchServicesQuery { SearchTerm = q });

        return result.Match(
            onSuccess: data => data.Count == 0
                ? (IActionResult)Ok(new
                {
                    success = true,
                    data = data,
                    message = $"لا توجد خدمات تطابق \"{q}\""
                })
                : Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }
}