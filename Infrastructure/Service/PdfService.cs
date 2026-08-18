using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs;
using EGovServices.Application.DTOs.CivilRecord;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EGovServices.Infrastructure.Service;

/// <summary>
/// PdfService — تقديم خدمات إنتاج ملفات الـ PDF الرسمية
/// (خلاصة السجل العدلي + بيان القيد الفردي)، بتصميمين مطابقين
/// لملفات HTML المرجعية، ويشتركان بنفس ملف الباترن (pattern.png).
/// </summary>
public sealed class PdfService : IPdfService
{
    private readonly string _basePath;
    private readonly byte[]? _logoBytes;
    private readonly byte[]? _patternBytes;
    private readonly IQrCodeService _qrCodeService;
    private readonly IVerificationTokenService _tokenService;
    private const string FontFamily = "Qomra";

    public PdfService(
        IConfiguration config,
        IQrCodeService qrCodeService,
        IVerificationTokenService tokenService,
        IHostEnvironment env)
    {
        _qrCodeService = qrCodeService;
        _tokenService = tokenService;

        _basePath = config["PdfStorage:BasePath"] ?? Path.Combine(
            Directory.GetCurrentDirectory(), "Certificates");

        var webRoot = Path.Combine(env.ContentRootPath, "wwwroot");

        var fontsPath = config["PdfStorage:FontsPath"] ?? Path.Combine(webRoot, "fonts");
        RegisterFontsOnce(fontsPath);

        var imagesPath = config["PdfStorage:ImagesPath"]
            ?? Path.Combine(webRoot, "images", "pdf");

        _logoBytes = TryReadFile(Path.Combine(imagesPath, "logo.png"));
        _patternBytes = TryReadFile(Path.Combine(imagesPath, "pattern.png"));

        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── تسجيل الخط مرة واحدة فقط طوال عمر التطبيق ────────────────────
    private static bool _fontsRegistered;
    private static readonly object FontLock = new();

    private static void RegisterFontsOnce(string fontsPath)
    {
        if (_fontsRegistered) return;

        lock (FontLock)
        {
            if (_fontsRegistered) return;

            RegisterFontFile(Path.Combine(fontsPath, "itfQomraArabic-Regular.ttf"));
            RegisterFontFile(Path.Combine(fontsPath, "itfQomraArabic-Bold.ttf"));

            _fontsRegistered = true;
        }
    }

    private static void RegisterFontFile(string path)
    {
        if (!File.Exists(path)) return;

        using var stream = File.OpenRead(path);
        FontManager.RegisterFontWithCustomName(FontFamily, stream);
    }

    private static byte[]? TryReadFile(string path) =>
        File.Exists(path) ? File.ReadAllBytes(path) : null;

    // ════════════════════════════════════════════════════════════════
    // 1. شهادة عدم المحكومية — مطابقة لملف HTML والنموذج الرسمي
    // ════════════════════════════════════════════════════════════════
    public async Task<string> GenerateClearanceCertificateAsync(ClearanceCertificatePdfData data)
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);

        var safeRef = data.ReferenceNumber.Replace("-", "_");
        var dateStamp = data.IssueDate.ToString("yyyyMMdd");
        var fileName = $"cert_{safeRef}_{dateStamp}.pdf";
        var fullPath = Path.Combine(_basePath, fileName);

        var qrBytes = _qrCodeService.GenerateQrCodeBytes(data.VerificationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.ContentFromRightToLeft();
                page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(9.5f));

                page.Background().Element(bg =>
                {
                    if (_patternBytes is not null)
                        bg.Image(_patternBytes).FitUnproportionally();
                });

                page.Content().PaddingHorizontal(40).PaddingTop(30).Column(col =>
                {
                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(ar =>
                        {
                            ar.Item().AlignRight().Text("الجمهورية العربية السورية").Bold().FontSize(9.5f);
                            ar.Item().AlignRight().Text("وزارة الــــــداخـلــــيــــــــــــــــة").FontSize(8.5f);
                            ar.Item().AlignRight().Text("إدارة المـــباحث الجنائيــــــة").FontSize(8.5f);
                        });

                        row.ConstantItem(150).Column(center =>
                        {
                            if (_logoBytes is not null)
                                center.Item().AlignCenter().Height(45).Image(_logoBytes).FitArea();

                            center.Item().AlignCenter().PaddingTop(3)
                                .Text("خلاصة سجل عدلي").Bold().FontSize(13.5f);
                        });

                        row.RelativeItem().Column(en =>
                        {
                            en.Item().AlignLeft().Text("SYRIAN ARAB REPUBLIC").Bold().FontSize(8.5f);
                            en.Item().AlignLeft().Text("MINISTRY OF INTERIOR").FontSize(8f);
                            en.Item().AlignLeft().Text("CRIMINAL INVESTIGATIONS DIRECTORATE").FontSize(6.5f);
                        });
                    });

                    col.Item().PaddingBottom(12).LineHorizontal(0.8f).LineColor(Colors.Grey.Medium);

                    col.Item().PaddingBottom(18).Row(row =>
                    {
                        row.RelativeItem().Row(r =>
                        {
                            r.AutoItem().Text(t =>
                            {
                                t.Span("التاريخ : ").Bold().FontSize(9f);
                                t.Span($"{GetHijriDate(data.IssueDate)} هـ      ").FontSize(9f);
                                t.Span("الموافق : ").Bold().FontSize(9f);
                                t.Span($"{data.IssueDate:yyyy/MM/dd}").FontSize(9f);
                            });
                        });

                        row.ConstantItem(50).Height(50).Image(qrBytes);
                    });

                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().PaddingLeft(10).Element(e => DottedField(e, "الاسم :", data.FirstName));
                        row.RelativeItem().PaddingRight(10).Element(e => DottedField(e, "النسبة :", data.LastName));
                    });

                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().PaddingLeft(10).Element(e => DottedField(e, "اسم الأب :", data.FatherName));
                        row.RelativeItem().PaddingRight(10).Element(e => DottedField(e, "اسم الأم :", data.MotherName));
                    });

                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().PaddingLeft(10).Element(e => DottedField(e, "محل الولادة :", data.PlaceOfBirth));
                        row.RelativeItem().PaddingRight(10).Element(e => DottedField(e, "تاريخ الولادة :", data.BirthDate?.ToString("yyyy/MM/dd")));
                    });

                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().PaddingLeft(10).Element(e => DottedField(e, "المحافظة المقيد بها :", data.RecordPlace));
                        row.RelativeItem().PaddingRight(10).Element(e => DottedField(e, "المحل ورقم القيد :", $"{data.RecordPlace} / {data.RecordNumber}"));
                    });

                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().PaddingLeft(10).Element(e => DottedField(e, "الجنسية :", "سوري"));
                        row.RelativeItem().PaddingRight(10).Element(e => DottedField(e, "محل الإقامة الحالي :", data.Address));
                    });

                    col.Item().PaddingBottom(20).Row(row =>
                    {
                        row.RelativeItem().Element(e => DottedField(e, "رقم البطاقة الشخصية أو قيد النفوس :", data.NationalNumber, labelWidth: 185));
                    });

                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2.5f);
                            c.RelativeColumn(2.5f);
                            c.RelativeColumn(2.5f);
                            c.RelativeColumn(2.5f);
                        });

                        string[] headers = ["اسم المحكمة", "الجرم", "تاريخ ورقم الحكم", "العقوبة"];
                        foreach (var header in headers)
                        {
                            table.Cell().Border(0.8f).BorderColor(Colors.Grey.Medium)
                                .Background("#F3F3F3").Padding(6)
                                .AlignCenter().Text(header).Bold().FontSize(8.5f);
                        }

                        if (data.CriminalRecords == null || data.CriminalRecords.Count == 0)
                        {
                            TableCell(table, "-");
                            TableCell(table, "-");
                            TableCell(table, "-");
                            TableCell(table, "غير محكوم", bold: true);
                        }
                        else
                        {
                            foreach (var record in data.CriminalRecords)
                            {
                                TableCell(table, "المحكمة المختصة");
                                TableCell(table, record.CrimeDescription);
                                TableCell(table, record.JudgmentDate.ToString("yyyy/MM/dd"));
                                TableCell(table, record.IsActive ? "قيد التنفيذ" : "منتهية", bold: true);
                            }
                        }
                    });
                });

                page.Footer().Background("#042522").PaddingVertical(10).PaddingHorizontal(15)
                    .AlignCenter()
                    .Text("هذه الوثيقة صالحة لمدة ثلاثة أشهر من تاريخ المنح")
                    .FontColor(Colors.White).FontSize(8.5f).Bold();
            });
        });

        await Task.Run(() => document.GeneratePdf(fullPath));
        return fullPath;
    }

    // ── Helpers خاصة بشهادة عدم المحكومية ────────────────────────────
    private static void DottedField(IContainer container, string label, string? value, float labelWidth = 110)
    {
        container.Row(row =>
        {
            row.ConstantItem(labelWidth)
               .AlignRight()
               .Text(label)
               .Bold()
               .FontSize(8.5f)
               .FontColor(Colors.Grey.Darken3);

            row.RelativeItem()
               .BorderBottom(0.8f)
               .BorderColor(Colors.Grey.Medium)
               .PaddingHorizontal(4)
               .AlignRight()
               .Text(string.IsNullOrWhiteSpace(value) ? "" : value)
               .Bold()
               .FontSize(9f);
        });
    }

    private static void TableCell(TableDescriptor table, string text, bool bold = false)
    {
        var cell = table.Cell().Border(0.8f).BorderColor(Colors.Grey.Medium)
            .Padding(6).AlignCenter();

        if (bold) cell.Text(text).Bold().FontSize(8.5f);
        else cell.Text(text).FontSize(8.5f);
    }

    private static string GetHijriDate(DateOnly date)
    {
        try
        {
            var calendar = new System.Globalization.HijriCalendar();
            var dt = date.ToDateTime(TimeOnly.MinValue);
            return $"{calendar.GetYear(dt)}/{calendar.GetMonth(dt):D2}/{calendar.GetDayOfMonth(dt):D2}";
        }
        catch
        {
            return "1448/02/07";
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 2. إخراج القيد الفردي — التصميم الجديد المطابق لـ file2.html
    //    يستخدم نفس _patternBytes المستخدم بشهادة عدم المحكومية
    // ════════════════════════════════════════════════════════════════
    public async Task<string> GenerateCivilRecordAsync(CivilRecordPdfData data)
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);

        var safeRef = data.ReferenceNumber.Replace("-", "_");
        var fileName = $"civil_{safeRef}_{DateTime.UtcNow:yyyyMMdd}.pdf";
        var fullPath = Path.Combine(_basePath, fileName);

        var qrBytes = _qrCodeService.GenerateQrCodeBytes(data.VerificationToken);
        const string Teal = "#042522";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.ContentFromRightToLeft();
                page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(9.5f));

                page.Background().Element(bg =>
                {
                    if (_patternBytes is not null)
                        bg.Image(_patternBytes).FitUnproportionally();
                });

                page.Content().PaddingHorizontal(40).PaddingTop(30).Column(col =>
                {
                    col.Item().PaddingBottom(12).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                        .PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Column(ar =>
                            {
                                ar.Item().AlignRight().Text("الجمهورية العربية الســـوريـــة").Bold().FontSize(9.5f);
                                ar.Item().AlignRight().Text("وزارة الــــــداخـلــــيــــــــــــــــــــــة").FontSize(8.5f);
                                ar.Item().AlignRight().Text("الإدارة العامة للأحوال المدنية").FontSize(8.5f);
                            });

                            row.ConstantItem(140).Column(center =>
                            {
                                if (_logoBytes is not null)
                                    center.Item().AlignCenter().Height(45).Image(_logoBytes).FitArea();

                                center.Item().AlignCenter().PaddingTop(3)
                                    .Text("بيان قيد فردي مدني").Bold().FontSize(13);
                            });

                            row.RelativeItem().Column(doc =>
                            {
                                doc.Item().AlignLeft().Width(48).Height(48).Image(qrBytes);
                                doc.Item().AlignLeft().Text("للتحقق عبر التطبيق").FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                                doc.Item().AlignLeft().Text(data.DocumentSerial).Bold().FontSize(7.5f);
                                doc.Item().AlignLeft().Text($"تاريخ الإصدار: {data.IssueDate}").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                            });
                        });

                    col.Item().PaddingTop(6).Column(inner =>
                    {
                        inner.Spacing(12);

                        inner.Item().Element(c => SectionBlock(c, Teal, "بيانات الهوية الشخصية", [
                            ("الرقم الوطني",     data.NationalNumber),
                            ("الاسم",            data.FirstName),
                            ("النسبة (اللقب)",   data.LastName),
                            ("اسم الأب",         data.FatherName),
                            ("اسم الأم ونسبتها", data.MotherFullName),
                            ("الجنس",            (data.Gender == "Male")?"ذكر":"أنثى")
                        ]));

                        inner.Item().Element(c => SectionBlock(c, Teal, "بيانات الولادة والجنسية", [
                            ("محل الولادة",  data.PlaceOfBirth),
                            ("تاريخ الولادة", data.DateOfBirth),
                            ("الجنسية",      "سوري"),
                            ("الدين",        data.Religion)
                        ]));

                        inner.Item().Element(c => SectionBlock(c, Teal, "بيانات القيد المدني", [
                            ("الوضع العائلي", data.MaritalStatus),
                            ("محل القيد",     data.RecordPlace),
                            ("رقم القيد",     data.RecordNumber),
                            ("ملاحظات",       string.IsNullOrWhiteSpace(data.Remarks) ? "-" : data.Remarks)
                        ]));
                    });
                });

                page.Footer().PaddingHorizontal(40).PaddingBottom(20).PaddingTop(6)
                    .BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(6)
                    .Row(row =>
                    {
                        row.RelativeItem().Text($"تاريخ الطباعة: {data.PrintDate}")
                            .FontSize(7.5f).FontColor(Colors.Grey.Darken2);

                        row.RelativeItem().AlignLeft()
                            .Text($"بيان صادر عن النظام الإلكتروني للشؤون المدنية  |  رقم الطلب: {data.ReferenceNumber}")
                            .FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                    });
            });
        });

        await Task.Run(() => document.GeneratePdf(fullPath));
        return fullPath;
    }

    // ── Helper: قسم بعنوان داكن + صفوف label/value بخلفية شفافة ──────
    private static void SectionBlock(
        IContainer container, string accentColor, string title, (string Label, string Value)[] rows)
    {
        container.Column(col =>
        {
            col.Item().Background(accentColor).Padding(6)
                .Text(title).Bold().FontSize(9.5f).FontColor(Colors.White);

            col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Column(table =>
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    var (label, value) = rows[i];
                    var isLast = i == rows.Length - 1;

                    table.Item()
                        .BorderBottom(isLast ? 0 : 1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(6)                              // ← بدون .Background() إطلاقاً
                        .Row(r =>
                        {
                            r.ConstantItem(160).Text(label).Bold().FontSize(9).FontColor(Colors.Grey.Darken3);
                            r.RelativeItem().Text(string.IsNullOrWhiteSpace(value) ? "—" : value)
                                .Bold().FontSize(9);
                        });
                }
            });
        });
    }
}
