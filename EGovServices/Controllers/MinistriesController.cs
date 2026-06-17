using EGovServices.Application.Features.Ministries.Queries.GetMinistries;
using EGovServices.Application.Features.Ministries.Queries.GetMinistryBranches;
using EGovServices.Application.Features.Ministries.Queries.GetMinistryById;
using EGovServices.Application.Features.Ministries.Queries.GetMinistryServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGovServices.API.Controllers;

/// <summary>
/// تدفق الشاشة الرئيسية:
///
///   GET /api/ministries                    ← الشاشة الرئيسية (كروت الوزارات)
///       ↓ يختار وزارة
///   GET /api/ministries/{id}               ← تفاصيل الوزارة
///   GET /api/ministries/{id}/services      ← قائمة خدمات الوزارة
///       ↓ يختار خدمة
///   GET /api/services/{serviceId}/form-schema  ← النموذج (موجود مسبقاً)
///       ↓ يملأ النموذج ويرسل
///   POST /api/services/{serviceId}/submit      ← التقديم (موجود مسبقاً)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MinistriesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// الشاشة الرئيسية — قائمة كل الوزارات النشطة مع عدد خدمات كل منها.
    ///
    /// GET /api/ministries
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await mediator.Send(new GetMinistriesQuery());

        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>
    /// تفاصيل وزارة واحدة (الاسم، الوصف، عدد الخدمات، عدد الفروع).
    ///
    /// GET /api/ministries/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await mediator.Send(new GetMinistryByIdQuery(id));

        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }

    /// <summary>
    /// خدمات وزارة معينة — تُعرض بعد اختيار الوزارة.
    ///
    /// GET /api/ministries/{id}/services
    /// </summary>
    [HttpGet("{id:guid}/services")]
    public async Task<IActionResult> GetServices(Guid id)
    {
        var result = await mediator.Send(new GetMinistryServicesQuery
        {
            MinistryId = id
        });

        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }

    /// <summary>
    /// فروع وزارة معينة مع الموقع الجغرافي.
    ///
    /// GET /api/ministries/{id}/branches
    /// </summary>
    [HttpGet("{id:guid}/branches")]
    public async Task<IActionResult> GetBranches(Guid id)
    {
        var result = await mediator.Send(new GetMinistryBranchesQuery(id));

        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }
}
