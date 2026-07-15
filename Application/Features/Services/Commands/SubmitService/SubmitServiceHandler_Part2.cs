using EGovServices.Application.Common;
using EGovServices.Application.DTOs.FormSchema;
using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EGovServices.Application.Features.Services.Commands.SubmitService;

public sealed partial class SubmitServiceHandler
{
    private static Result ValidateFormData(
        Dictionary<string, object> formData, List<ServiceFormField> fields)
    {
        foreach (var field in fields.Where(f => f.IsRequired))
        {
            if (!formData.TryGetValue(field.FieldName, out var val) ||
                val is null || (val is string s && string.IsNullOrWhiteSpace(s)))
                return Result.Failure($"الحقل '{field.Label}' مطلوب");
        }

        var defined = fields.Select(f => f.FieldName).ToHashSet();
        var extra = formData.Keys.Where(k => !defined.Contains(k)).ToList();
        if (extra.Count != 0)
            return Result.Failure($"حقول غير معروفة: {string.Join(", ", extra)}");

        foreach (var field in fields)
        {
            if (!formData.TryGetValue(field.FieldName, out var value) || value is null) continue;
            var result = ValidateField(field, value);
            if (!result.IsSuccess) return result;
        }
        return Result.Success();
    }

    private static Result ValidateField(ServiceFormField field, object value)
    {
        if (string.IsNullOrWhiteSpace(field.ValidationRules)) return Result.Success();

        ValidationRulesDto? rules;
        try { rules = JsonSerializer.Deserialize<ValidationRulesDto>(field.ValidationRules,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { return Result.Success(); }

        if (rules is null) return Result.Success();

        var str = value?.ToString() ?? string.Empty;

        if (rules.MinLength is { } min && str.Length < min)
            return Result.Failure(rules.CustomMessage ?? $"{field.Label}: الحد الأدنى {min} حرف");

        if (rules.MaxLength is { } max && str.Length > max)
            return Result.Failure(rules.CustomMessage ?? $"{field.Label}: الحد الأقصى {max} حرف");

        if (rules.Length is { } exact && str.Length != exact)
            return Result.Failure(rules.CustomMessage ?? $"{field.Label}: يجب أن يكون {exact} حرف بالضبط");

        if (!string.IsNullOrWhiteSpace(rules.Pattern))
        {
            try { if (!Regex.IsMatch(str, rules.Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100)))
                return Result.Failure(rules.CustomMessage ?? $"{field.Label}: صيغة غير صحيحة"); }
            catch (RegexMatchTimeoutException) { return Result.Failure($"{field.Label}: صيغة غير صحيحة"); }
            catch (ArgumentException) { }
        }

        if (field.FieldType == "number" && decimal.TryParse(str, out var num))
        {
            if (rules.MinValue is { } minV && num < minV) return Result.Failure(rules.CustomMessage ?? $"{field.Label}: الحد الأدنى {minV}");
            if (rules.MaxValue is { } maxV && num > maxV) return Result.Failure(rules.CustomMessage ?? $"{field.Label}: الحد الأقصى {maxV}");
        }

        if (field.FieldType == "date" && DateTime.TryParse(str, out var date))
        {
            if (!string.IsNullOrWhiteSpace(rules.MinDate) && ParseDate(rules.MinDate) is { } mn && date < mn)
                return Result.Failure(rules.CustomMessage ?? $"{field.Label}: التاريخ لا يمكن أن يكون قبل {mn:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(rules.MaxDate) && ParseDate(rules.MaxDate) is { } mx && date > mx)
                return Result.Failure(rules.CustomMessage ?? $"{field.Label}: التاريخ لا يمكن أن يكون بعد {mx:yyyy-MM-dd}");
        }

        if (field.FieldType == "file" && !Guid.TryParse(str, out _))
            return Result.Failure($"{field.Label}: معرف المرفق غير صحيح");

        return Result.Success();
    }

    private static DateTime? ParseDate(string s) => s.ToLowerInvariant() switch
    {
        "today" => DateTime.Today,
        var x when x.StartsWith("today+") && int.TryParse(x[6..], out var d) => DateTime.Today.AddDays(d),
        var x when x.StartsWith("today-") && int.TryParse(x[6..], out var d) => DateTime.Today.AddDays(-d),
        var x when DateTime.TryParse(x, out var abs) => abs.Date,
        _ => null
    };

    private Task<string> GenerateReferenceNumber(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // 1. استخدام طابع زمني دقيق جداً (السنة والشهر واليوم والساعة والدقيقة)
        var timestamp = now.ToString("yyyyMMddHHmm");

        // 2. توليد جزء عشوائي فريد ومختصر (مكون من 6 محارف) لمنع أي تكرار متزامن
        var uniquePart = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        // النتيجة ستكون بشكل منظم وثابت مثل: REQ-202606251430-BF3A12
        var refNumber = $"REQ-{timestamp}-{uniquePart}";

        // بما أننا لا نستخدم await هنا، نُرجع النتيجة كـ Task مكتمل مباشرة لرفع الأداء
        return Task.FromResult(refNumber);
    }
}
