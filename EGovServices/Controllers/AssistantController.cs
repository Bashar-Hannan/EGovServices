using EGovServices.Application.DTOs.Assistant;
using EGovServices.Application.Features.Assistant.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/assistant")]
[Authorize]                        // المواطن يجب يكون مسجل دخول
public class AssistantController : ControllerBase
{
    private readonly IMediator _mediator;

    public AssistantController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// يرسل رسالة للمساعد الذكي ويرجع الرد.
    /// الـ Frontend يرسل تاريخ المحادثة كاملاً مع كل طلب.
    ///
    /// POST /api/assistant/chat
    /// Body:
    /// {
    ///   "messages": [
    ///     { "role": "user",      "content": "ما هي خدمات الجوازات؟" },
    ///     { "role": "assistant", "content": "خدمات الجوازات تشمل..." },
    ///     { "role": "user",      "content": "وكم رسومها؟" }
    ///   ]
    /// }
    ///
    /// Response:
    /// {
    ///   "reply": "رسوم تجديد جواز السفر هي 150 ريال..."
    /// }
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (request.Messages.Count == 0)
            return BadRequest(new { message = "يجب إرسال رسالة واحدة على الأقل" });

        var result = await _mediator.Send(
            new ChatWithAssistantCommand(request.Messages),
            cancellationToken);

        if (!result.IsSuccess)
            return StatusCode(503, new { message = result.Error });

        return Ok(result.Value);
    }
}
