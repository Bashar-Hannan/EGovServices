using EGovServices.Application.Features.Files;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public sealed class FilesController(IMediator mediator, IAppDbContext context) : ControllerBase
{
    /// <summary>
    /// رفع ملف (صورة أو PDF) أثناء تعبئة النموذج.
    ///
    /// POST /api/files/upload
    /// Content-Type: multipart/form-data
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await mediator.Send(
            new UploadFileCommand(file, userId),
            cancellationToken);

        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>
    /// عرض/تحميل ملف مرفوع — يستخدمه الموظف لرؤية الصور المرفوعة مع الطلب.
    ///
    /// GET /api/files/{fileId}
    ///
    /// يرجع الملف مباشرة كـ binary — الفرونت يعرضه في img tag أو يفتحه في tab جديد.
    /// مثال في Flutter/Web:
    ///   Image.network('/api/files/{fileId}', headers: { 'Authorization': 'Bearer ...' })
    /// </summary>
    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> GetFile(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        // ── 1. جلب بيانات الملف من DB ─────────────────────────────────
        var uploadedFile = await context.UploadedFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

        if (uploadedFile is null)
            return NotFound(new { success = false, message = "الملف غير موجود" });

        // ── 2. التأكد من وجود الملف على القرص ────────────────────────
        if (!System.IO.File.Exists(uploadedFile.FilePath))
            return NotFound(new { success = false, message = "الملف غير موجود على السيرفر" });

        // ── 3. إرجاع الملف ────────────────────────────────────────────
        var fileBytes = await System.IO.File.ReadAllBytesAsync(
            uploadedFile.FilePath, cancellationToken);

        // inline = يعرضه في المتصفح مباشرة (لا يُحمَّل)
        Response.Headers.Append(
            "Content-Disposition",
            $"inline; filename=\"{uploadedFile.OriginalFileName}\"");

        return File(fileBytes, uploadedFile.ContentType);
    }
}
