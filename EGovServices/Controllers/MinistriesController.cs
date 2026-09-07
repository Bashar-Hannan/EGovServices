using EGovServices.Application.Features.Ministries.Queries.GetMinistries;
using EGovServices.Application.Features.Ministries.Queries.GetMinistryBranches;
using EGovServices.Application.Features.Ministries.Queries.GetMinistryById;
using EGovServices.Application.Features.Ministries.Queries.GetMinistryServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MinistriesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await mediator.Send(new GetMinistriesQuery());
        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await mediator.Send(new GetMinistryByIdQuery(id));
        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }

    [HttpGet("{id:guid}/services")]
    public async Task<IActionResult> GetServices(Guid id)
    {
        var result = await mediator.Send(new GetMinistryServicesQuery
        {
            MinistryId = id
        });
        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }

    /// <summary>
    /// فروع الوزارة — يدعم الآن فلترة اختيارية بالمحافظة.
    ///
    /// GET /api/ministries/{id}/branches                    ← كل الفروع
    /// GET /api/ministries/{id}/branches?governorate=حلب     ← فروع حلب فقط
    ///
    /// تستخدمها خدمة "عرض المستشفيات الحكومية" تحت وزارة الصحة —
    /// كل مستشفى مُخزَّن كـ Branch، وعمود City يمثّل المحافظة.
    /// </summary>
    [HttpGet("{id:guid}/branches")]
    public async Task<IActionResult> GetBranches(Guid id, [FromQuery] string? governorate)
    {
        var result = await mediator.Send(new GetMinistryBranchesQuery
        {
            MinistryId = id,
            Governorate = governorate
        });
        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }
}
