using EGovServices.Application.Common;
using EGovServices.Application.DTOs.Assistant;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace EGovServices.Application.Features.Assistant.Commands;

// ── Command ───────────────────────────────────────────────────────────────────
public record ChatWithAssistantCommand(
    List<ChatMessage> Messages
) : IRequest<Result<ChatResponse>>;

// ── Handler ───────────────────────────────────────────────────────────────────
public class ChatWithAssistantHandler
    : IRequestHandler<ChatWithAssistantCommand, Result<ChatResponse>>
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ChatWithAssistantHandler(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey غير موجود في الإعدادات");
    }

    public async Task<Result<ChatResponse>> Handle(
        ChatWithAssistantCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Messages.Count == 0)
            return Result<ChatResponse>.Failure("الرسائل فارغة");

        // ── بناء الطلب بصيغة Gemini API ──────────────────────────────
        var geminiRequest = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = AssistantSystemPrompt.Text } }
            },
            contents = request.Messages.Select(m => new
            {
                // Gemini يستخدم "model" بدلاً من "assistant"
                role  = m.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = m.Content } }
            }).ToArray()
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent?key={_apiKey}";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, geminiRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // قراءة تفاصيل الخطأ من جوجل لتعرف المشكلة بالضبط
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result<ChatResponse>.Failure($"رمز الخطأ: {response.StatusCode} \nالتفاصيل: {errorBody}");
            }
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc  = JsonDocument.Parse(json);

            var reply = doc
                .RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(reply))
                return Result<ChatResponse>.Failure("لم يتمكن المساعد من توليد رد");

            return Result<ChatResponse>.Success(new ChatResponse(reply));
        }
        catch (Exception)
        {
            return Result<ChatResponse>.Failure("حدث خطأ أثناء التواصل مع المساعد");
        }
    }
}
