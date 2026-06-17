using EGovServices.Domain.Entities;
using System.Security.Claims;
using System.Text;

namespace EGovServices.Application.Features.ClearanceCertificate;

/// <summary>
/// Part 2: Helper methods for CreateClearanceCertificateHandler
/// </summary>
public sealed partial class CreateClearanceCertificateHandler
{
    /// <summary>
    /// Extracts NationalNumber from JWT claims stored in IHttpContextAccessor.
    /// The JWT token contains a claim "NationalNumber" added during login
    /// (see JwtService.cs: new Claim("NationalNumber", user.NationalNumber))
    /// </summary>
    private string? ExtractNationalNumberFromClaims()
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user is null) return null;

        // Try the custom "NationalNumber" claim first
        var nationalNumber = user.FindFirst("NationalNumber")?.Value;

        // Fallback: not found — return null
        return string.IsNullOrWhiteSpace(nationalNumber) ? null : nationalNumber;
    }

    /// <summary>
    /// Analyzes the list of criminal records and builds a human-readable result.
    ///
    /// Logic:
    /// - No records at all     → "لا توجد سوابق جنائية"
    /// - Only inactive records → "توجد سجلات سابقة (منتهية الحكم)"
    /// - Any active records    → list each crime description
    ///
    /// Returns: (checkResultText, hasActiveCrimes)
    /// </summary>
    private static (string checkResult, bool hasActiveCrimes)
        BuildCheckResult(List<CriminalRecord> records)
    {
        // Case 1: No records at all
        if (records.Count == 0)
        {
            return (
                checkResult: "لا توجد سوابق جنائية مسجلة — نظيف السجل",
                hasActiveCrimes: false
            );
        }

        var activeRecords = records.Where(r => r.IsActive).ToList();

        // Case 2: Records exist but all are settled (IsActive = false)
        if (activeRecords.Count == 0)
        {
            return (
                checkResult: "توجد سجلات سابقة منتهية الحكم — لا توجد موانع حالية",
                hasActiveCrimes: false
            );
        }

        // Case 3: One or more active criminal records
        var sb = new StringBuilder();
        sb.AppendLine("يوجد سجل جنائي نشط:");
        sb.AppendLine();

        for (int i = 0; i < activeRecords.Count; i++)
        {
            var record = activeRecords[i];
            sb.AppendLine(
                $"{i + 1}. {record.CrimeDescription} " +
                $"(تاريخ الحكم: {record.JudgmentDate:dd/MM/yyyy})");
        }

        return (
            checkResult: sb.ToString().Trim(),
            hasActiveCrimes: true
        );
    }
}
