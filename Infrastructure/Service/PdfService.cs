using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EGovServices.Infrastructure.Service;

public sealed class PdfService(IConfiguration config) : IPdfService
{
    private readonly string _basePath = GetBasePath(config);

    private static string GetBasePath(IConfiguration config)
    {
        var configured = config["PdfStorage:BasePath"];

        if (string.IsNullOrEmpty(configured))
            return Path.Combine(AppContext.BaseDirectory, "Certificates");

        // لو المسار نسبي اربطه بمجلد التطبيق
        if (!Path.IsPathRooted(configured))
            return Path.Combine(AppContext.BaseDirectory, configured);

        // لو مسار مطلق استخدمه مباشرة
        return configured;
    }

    public async Task<string> GenerateClearanceCertificateAsync(ClearanceCertificatePdfData data)
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);

        var fileName = $"cert_{data.ReferenceNumber.Replace("-", "_")}_{data.IssueDate:yyyyMMdd}.pdf";
        var fullPath = Path.Combine(_basePath, fileName);

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                // استخدام خط يدعم العربية (تأكد من تنصيبه على السيرفر)
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // 1. الترويسة العليا (مقسمة لثلاثة أقسام حسب الصورة)
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Row(row =>
                    {
                        // القسم الأيسر: الصلاحية والتاريخ
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("تعتبر صالحة لمدة ثلاثة أشهر فقط").FontSize(9);
                            col.Item().Text($"الرقم: {data.ReferenceNumber}");
                            col.Item().Text($"التاريخ: {data.IssueDate:yyyy/MM/dd}");
                            col.Item().Text("الموافق: 0"); // أو أي قيمة إضافية
                        });

                        // القسم الأوسط: الشعار الوطني
                        row.ConstantItem(100).AlignCenter().Column(col =>
                        {
                            col.Item().Height(60).Placeholder(); // هنا يوضع شعار النسر السوري
                            col.Item().PaddingTop(2).AlignCenter().Text("№ 010000").FontSize(12).Bold();
                        });

                        // القسم الأيمن: الجهة المصدرة
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("الجمهورية العربية السورية").Bold().FontSize(12);
                            col.Item().Text("وزارة الداخلية").Bold();
                            col.Item().Text("فرع الأمن الجنائي");
                            col.Item().Text("مكتب السجل العدلي");
                        });
                    });

                    headerCol.Item().PaddingTop(10).AlignCenter().Text("خلاصة السجل العدلي")
                        .FontSize(20).Bold();
                });

                // 2. محتوى البيانات الشخصية والـ QR
                page.Content().PaddingVertical(20).Column(contentCol =>
                {
                    contentCol.Item().Row(row =>
                    {
                        // الـ QR Code على اليسار
                        row.ConstantItem(100).Column(qrCol => {
                            qrCol.Item().Height(80).Width(80).Background(Colors.Grey.Lighten3).AlignCenter().Text("QR Code");
                        });

                        // جدول البيانات الشخصية (عمودين كما في الصورة)
                        row.RelativeItem().PaddingLeft(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); // القيمة يسار
                                columns.ConstantColumn(120); // التسمية يسار
                                columns.RelativeColumn(); // القيمة يمين
                                columns.ConstantColumn(80); // التسمية يمين
                            });

                            // السطر الأول
                            table.Cell().Text("نموذج خـ 11"); // محل القيد
                            table.Cell().AlignRight().Text(" :محل ورقم القيد");
                            table.Cell().Text(data.FullName);
                            table.Cell().AlignRight().Text(" :الاسم");

                            // السطر الثاني
                            table.Cell().Text("دمشق");
                            table.Cell().AlignRight().Text(" :المحافظة المقيد بها");
                            table.Cell().Text("كنية النموذج");
                            table.Cell().AlignRight().Text(" :النسبة");

                            // السطر الثالث
                            table.Cell().Text("سورية");
                            table.Cell().AlignRight().Text(" :الجنسية");
                            table.Cell().Text("اسم الأب");
                            table.Cell().AlignRight().Text(" :اسم الأب");

                            // السطر الرابع
                            table.Cell().Text("دمشق - المزة");
                            table.Cell().AlignRight().Text(" :محل الإقامة الحالي");
                            table.Cell().Text("اسم الأم");
                            table.Cell().AlignRight().Text(" :اسم الأم");

                            // السطر الخامس
                            table.Cell().Text(data.NationalNumber);
                            table.Cell().AlignRight().Text(" :رقم البطاقة الشخصية");
                            table.Cell().Text("دمشق 01/01/1990");
                            table.Cell().AlignRight().Text(" :محل وتاريخ الولادة");
                        });
                    });

                    // 3. جدول الأحكام (Court Table)
                    contentCol.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(); // العقوبة
                            columns.RelativeColumn(); // تاريخ ورقم الحكم
                            columns.RelativeColumn(); // الجرم
                            columns.RelativeColumn(); // اسم المحكمة
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).AlignCenter().Text("العقوبة");
                            header.Cell().BorderBottom(1).AlignCenter().Text("تاريخ ورقم الحكم");
                            header.Cell().BorderBottom(1).AlignCenter().Text("الجرم");
                            header.Cell().BorderBottom(1).AlignCenter().Text("اسم المحكمة");
                        });

                        // إذا كان هناك جرائم تضاف هنا، وإلا تترك فارغة
                        if (data.HasActiveCrimes)
                        {
                            table.Cell().AlignCenter().Text("سجن سنة");
                            table.Cell().AlignCenter().Text("2024/100");
                            table.Cell().AlignCenter().Text("سرقة");
                            table.Cell().AlignCenter().Text("بداية الجزاء");
                        }
                    });

                    // 4. مربع النتيجة (غير محكوم)
                    contentCol.Item().PaddingTop(30).AlignCenter().Width(200).Border(1.5f).Padding(10).Column(resCol =>
                    {
                        if (!data.HasActiveCrimes)
                        {
                            resCol.Item().AlignCenter().Text("غير محكوم").FontSize(22).Bold();
                        }
                        else
                        {
                            resCol.Item().AlignCenter().Text("عليه أحكام").FontSize(18).Bold().FontColor(Colors.Red.Medium);
                        }
                    });
                });

                // 5. التوقيع والختم السفلي
                page.Footer().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Height(50).Placeholder(); // ختم النسر السفلي
                    });

                    row.RelativeItem().AlignRight().PaddingTop(20).Column(col =>
                    {
                        col.Item().Text("مكتب السجل العدلي").Bold();
                    });
                });
            });
        });

        await Task.Run(() => document.GeneratePdf(fullPath));
        return fullPath;
    }
}