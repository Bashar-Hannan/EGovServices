using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs;
using EGovServices.Application.DTOs.CivilRecord;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EGovServices.Infrastructure.Service;

/// <summary>
/// النسخة النهائية من PdfService:
/// - QuestPDF بالكامل (بدون PuppeteerSharp) — يعمل على Shared Hosting
/// - QR Code يحتوي Token فقط (بدون URL) — الفلاتر يقرأه ويستدعي API مباشرة
/// - يعمل للوثيقتين: شهادة عدم المحكومية + إخراج القيد الفردي
/// </summary>
public sealed class PdfService(
    IConfiguration config,
    IQrCodeService qrCodeService,
    IVerificationTokenService tokenService)
    : IPdfService
{
    private readonly string _basePath =
        config["PdfStorage:BasePath"] ?? Path.Combine(
            Directory.GetCurrentDirectory(), "Certificates");

    // ════════════════════════════════════════════════════════════════
    // 1. شهادة عدم المحكومية + QR Token
    // ════════════════════════════════════════════════════════════════
    public async Task<string> GenerateClearanceCertificateAsync(
        ClearanceCertificatePdfData data)
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);

        var safeRef = data.ReferenceNumber.Replace("-", "_");
        var dateStamp = data.IssueDate.ToString("yyyyMMdd");
        var fileName = $"cert_{safeRef}_{dateStamp}.pdf";
        var fullPath = Path.Combine(_basePath, fileName);

        // QR يحتوي Token فقط — الفلاتر يقرأه ويستدعي API مباشرة
        var qrBytes = qrCodeService.GenerateQrCodeBytes(data.VerificationToken);

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        // العنوان
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().AlignCenter()
                                .Text("الجمهورية العربية السورية")
                                .Bold().FontSize(16);
                            inner.Item().AlignCenter()
                                .Text("وزارة الداخلية")
                                .Bold().FontSize(14);
                            inner.Item().Height(8);
                            inner.Item().AlignCenter()
                                .Text("شهادة عدم المحكومية")
                                .Bold().FontSize(18).Underline();
                            inner.Item().AlignCenter()
                                .Text($"رقم المرجع: {data.ReferenceNumber}")
                                .FontSize(11).FontColor(Colors.Grey.Darken2);
                        });

                        // QR Token — زاوية يمين الترويسة
                        row.ConstantItem(80).Column(qrCol =>
                        {
                            qrCol.Item().Width(70).Height(70).Image(qrBytes);
                            qrCol.Item().AlignCenter()
                                .Text("للتحقق من الوثيقة")
                                .FontSize(7).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    col.Item().Height(16);
                });

                page.Content().Column(col =>
                {
                    col.Item().BorderBottom(1).PaddingBottom(8).Column(inner =>
                    {
                        inner.Item().Text("بيانات المواطن").Bold().FontSize(13);
                        inner.Item().Height(6);
                        inner.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"الاسم الكامل:  {data.FullName}");
                            row.RelativeItem().Text($"رقم الهوية:    {data.NationalNumber}");
                        });
                    });

                    col.Item().Height(16);

                    col.Item().Column(inner =>
                    {
                        inner.Item().Text("نتيجة الفحص الجنائي").Bold().FontSize(13);
                        inner.Item().Height(8);

                        var boxColor = data.HasActiveCrimes ? Colors.Red.Lighten4 : Colors.Green.Lighten4;
                        var textColor = data.HasActiveCrimes ? Colors.Red.Darken3 : Colors.Green.Darken3;

                        col.Item()
                            .Background(boxColor).Border(1)
                            .BorderColor(data.HasActiveCrimes ? Colors.Red.Medium : Colors.Green.Medium)
                            .Padding(12)
                            .Text(data.CheckResult)
                            .FontColor(textColor).FontSize(12);
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().BorderTop(1).PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem()
                            .Text($"تاريخ الإصدار: {data.IssueDate:dd/MM/yyyy}");
                        row.RelativeItem().AlignCenter()
                            .Text("امسح QR للتحقق من صحة الوثيقة عبر التطبيق")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                        row.RelativeItem().AlignRight()
                            .Text("صادرة إلكترونياً — صالحة بدون توقيع")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        });

        await Task.Run(() => document.GeneratePdf(fullPath));
        return fullPath;
    }

    // ════════════════════════════════════════════════════════════════
    // 2. إخراج القيد الفردي + QR Token
    // ════════════════════════════════════════════════════════════════
    public async Task<string> GenerateCivilRecordAsync(CivilRecordPdfData data)
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);

        var safeRef = data.ReferenceNumber.Replace("-", "_");
        var fileName = $"civil_{safeRef}_{DateTime.UtcNow:yyyyMMdd}.pdf";
        var fullPath = Path.Combine(_basePath, fileName);

        // QR يحتوي Token فقط
        var qrBytes = qrCodeService.GenerateQrCodeBytes(data.VerificationToken);

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Background().Border(2).BorderColor("#2C5F2E").Padding(4)
                    .Border(1).BorderColor("#2C5F2E");

                page.Header().PaddingBottom(10).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        // معلومات الجمهورية (يمين)
                        row.RelativeItem().Column(side =>
                        {
                            side.Item().AlignRight()
                                .Text("الجمهورية العربية السورية")
                                .Bold().FontSize(9).FontColor("#2C5F2E");
                            side.Item().AlignRight()
                                .Text("وزارة الداخلية")
                                .FontSize(8).FontColor("#2C5F2E");
                            side.Item().AlignRight()
                                .Text("الإدارة العامة للشؤون المدنية")
                                .FontSize(8).FontColor("#2C5F2E");
                        });

                        // العنوان (وسط)
                        row.RelativeItem(1.3f).Column(center =>
                        {
                            center.Item().AlignCenter()
                                .Text("بيان قيد فردي مدني")
                                .Bold().FontSize(17).FontColor("#1A3D1C");
                            center.Item().AlignCenter().PaddingTop(3)
                                .Border(1).BorderColor("#2C5F2E").Padding(3)
                                .Text("بيانات القيد")
                                .Bold().FontSize(10).FontColor("#2C5F2E");
                        });

                        // QR Token + بيانات الوثيقة (يسار)
                        row.RelativeItem().Column(side =>
                        {
                            side.Item().AlignCenter()
                                .Width(65).Height(65).Image(qrBytes);
                            side.Item().AlignCenter()
                                .Text("للتحقق عبر التطبيق")
                                .FontSize(7).FontColor("#2C5F2E");
                            side.Item().Height(4);
                            side.Item().AlignLeft()
                                .Text($"رقم الوثيقة: {data.DocumentSerial}")
                                .FontSize(7.5f).FontColor("#2C5F2E");
                            side.Item().AlignLeft()
                                .Text($"تاريخ الإصدار: {data.IssueDate}")
                                .FontSize(7.5f).FontColor("#2C5F2E");
                        });
                    });

                    col.Item().PaddingTop(8).BorderBottom(2).BorderColor("#2C5F2E");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Element(c => SectionTitle(c, "بيانات الهوية الشخصية"));
                    col.Item().Element(c => DataTable(c, [
                        ("الرقم الوطني",     data.NationalNumber),
                        ("الاسم",            data.FirstName),
                        ("النسبة (اللقب)",   data.LastName),
                        ("اسم الأب",         data.FatherName),
                        ("اسم الأم ونسبتها", data.MotherFullName),
                        ("الجنس",            data.Gender)
                    ]));

                    col.Item().Element(c => SectionTitle(c, "بيانات الولادة والجنسية"));
                    col.Item().Element(c => DataTable(c, [
                        ("محل الولادة",  data.PlaceOfBirth),
                        ("تاريخ الولادة", data.DateOfBirth),
                        ("الجنسية",      "سورية"),
                        ("الدين",        data.Religion)
                    ]));

                    col.Item().Element(c => SectionTitle(c, "بيانات القيد المدني"));
                    col.Item().Element(c => DataTable(c, [
                        ("الوضع العائلي", data.MaritalStatus),
                        ("محل القيد",     data.RecordPlace),
                        ("رقم القيد",     data.RecordNumber),
                        ("ملاحظات",       string.IsNullOrWhiteSpace(data.Remarks) ? "—" : data.Remarks)
                    ]));
                });

                page.Footer().PaddingTop(8).Column(col =>
                {
                    col.Item().BorderTop(2).BorderColor("#2C5F2E").PaddingTop(6)
                        .Background("#EAF3EA").Padding(6).AlignCenter()
                        .Text($"بيان صادر عن النظام الإلكتروني للشؤون المدنية  |  " +
                              $"رقم الطلب: {data.ReferenceNumber}  |  " +
                              $"تاريخ الطباعة: {data.PrintDate}")
                        .FontSize(7.5f).FontColor("#1A3D1C");
                });
            });
        });

        await Task.Run(() => document.GeneratePdf(fullPath));
        return fullPath;
    }

    // ── Helpers ───────────────────────────────────────────────────────
    private static void SectionTitle(IContainer container, string title)
    {
        container
            .Background("#2C5F2E")
            .Padding(5)
            .Text(title)
            .Bold().FontSize(10).FontColor(Colors.White);
    }

    private static void DataTable(
        IContainer container,
        (string Label, string Value)[] rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(7);
            });

            for (int i = 0; i < rows.Length; i++)
            {
                var (label, value) = rows[i];
                var bgColor = i % 2 == 0 ? "#FFFFFF" : "#F7FBF7";

                table.Cell().Border(1).BorderColor("#2C5F2E")
                    .Background("#D4EBD4").Padding(5)
                    .Text(label).Bold().FontSize(9).FontColor("#1A3D1C");

                table.Cell().Border(1).BorderColor("#2C5F2E")
                    .Background(bgColor).Padding(5)
                    .Text(value ?? "—").FontSize(9.5f);
            }
        });
    }
}
