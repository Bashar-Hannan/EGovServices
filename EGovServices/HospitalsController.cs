using EGovServices.Application.Features.Hospitals.Queries.GetHospitalFilters;
using EGovServices.Application.Features.Hospitals.Queries.GetHospitals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGovServices.API.Controllers;

/// <summary>
/// خدمة "عرض المستشفيات الحكومية" — نوع الخدمة الرابع (ServiceType.Directory = 4).
/// هذا endpoint مخصص بالكامل ومستقل عن /api/services/{id}/schema
/// و /api/services/submit — لا يوجد نموذج تعبئة ولا ServiceRequest هنا،
/// فقط قائمة/خريطة قابلة للفلترة. الفرونت يتعرّف على النوع 4 من
/// حقل serviceType في الخدمة، ويستدعي هذا الـcontroller مباشرة
/// بدل التدفق العام للخدمات.
///
/// GET /api/hospitals/{ministryId}                                  ← كل المستشفيات
/// GET /api/hospitals/{ministryId}?governorate=حلب                  ← فلترة بالمحافظة
/// GET /api/hospitals/{ministryId}?specialty=قلبية                  ← فلترة بالاختصاص
/// GET /api/hospitals/{ministryId}?governorate=حلب&amp;specialty=عام  ← الاثنان معاً
/// GET /api/hospitals/{ministryId}/filters                          ← قوائم الفلاتر المتاحة
/// </summary>
[ApiController]
[Route("api/hospitals")]
[AllowAnonymous]
public sealed class HospitalsController(IMediator mediator) : ControllerBase
{
    [HttpGet("{ministryId:guid}")]
    public async Task<IActionResult> GetHospitals(
        Guid ministryId,
        [FromQuery] string? governorate,
        [FromQuery] string? specialty)
    {
        var result = await mediator.Send(new GetHospitalsQuery
        {
            MinistryId = ministryId,
            Governorate = governorate,
            Specialty = specialty
        });

        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    [HttpGet("{ministryId:guid}/filters")]
    public async Task<IActionResult> GetFilters(Guid ministryId)
    {
        var result = await mediator.Send(new GetHospitalFiltersQuery
        {
            MinistryId = ministryId
        });

        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }
}
