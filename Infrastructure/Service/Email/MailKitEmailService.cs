using EGovServices.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace EGovServices.Infrastructure.Service.Email;

/// <summary>
/// Production email service using MailKit + Gmail SMTP with optimized RTL template.
/// </summary>
public sealed class MailKitEmailService(IConfiguration config) : IEmailService
{
    public async Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
    {
        var message = new MimeMessage();

        // From
        message.From.Add(new MailboxAddress(
            "منصة الخدمات الحكومية",
            config["Email:From"]!));

        // To
        message.To.Add(new MailboxAddress(fullName, toEmail));

        // Subject
        message.Subject = "رمز التحقق — منصة الخدمات الحكومية";

        // Body — HTML template
        message.Body = new TextPart("html")
        {
            Text = BuildHtmlTemplate(fullName, otpCode)
        };

        // Send via SMTP
        using var client = new SmtpClient();

        await client.ConnectAsync(
            config["Email:Host"]!,
            int.Parse(config["Email:Port"]!),
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            config["Email:From"]!,
            config["Email:Password"]!);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static string BuildHtmlTemplate(string fullName, string otpCode)
    {
        // استخدام الـ Inline CSS يضمن ظهور التنسيق والاتجاه RTL في Gmail بشكل كامل وصحيح
        return $$"""
    <!DOCTYPE html>
    <html dir="rtl" lang="ar">
    <head>
      <meta charset="UTF-8">
      <meta name="viewport" content="width=device-width, initial-scale=1.0">
    </head>
    <body style="font-family: 'Segoe UI', Tahoma, Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; direction: rtl; text-align: right;">
      
      <div class="container" style="max-width: 500px; margin: auto; background: #ffffff; border-radius: 12px; padding: 30px; box-shadow: 0 2px 8px rgba(0,0,0,.1); border-top: 6px solid #1D9E75; direction: rtl;">
        
        <div class="header" style="text-align: center; border-bottom: 2px solid #f0f0f0; padding-bottom: 16px; margin-bottom: 20px;">
          <h2 style="color: #1D9E75; margin: 0; font-size: 22px;">منصة الخدمات الحكومية الرقمية</h2>
        </div>

        <p style="font-size: 16px; color: #333333; line-height: 1.6; margin: 0 0 12px 0;">السيد/ة <strong>{{fullName}}</strong>،</p>
        <p style="font-size: 15px; color: #555555; line-height: 1.6; margin: 0 0 20px 0;">لقد تلقينا طلباً لتأكيد هويتك وتفعيل حسابك على المنصة. يرجى استخدام رمز التحقق (OTP) التالي لإتمام العملية:</p>

        <div class="otp-box" style="text-align: center; margin: 24px 0; background-color: #f0fdf8; border: 2px dashed #1D9E75; border-radius: 8px; padding: 20px; direction: ltr;">
          <div class="otp-code" style="font-size: 38px; font-weight: bold; letter-spacing: 8px; color: #0F6E56; font-family: monospace;">{{otpCode}}</div>
          <div class="note" style="color: #1D9E75; font-size: 13px; text-align: center; margin-top: 8px; font-family: 'Segoe UI', Tahoma, sans-serif; direction: rtl;">صالح لمدة 10 دقائق فقط</div>
        </div>

        <p style="font-size: 14px; color: #666666; line-height: 1.6; margin: 20px 0 0 0;">إذا لم تقم بطلب هذا الرمز، فيرجى تجاهل هذا البريد الإلكتروني بأمان.</p>

        <div class="footer" style="text-align: center; margin-top: 30px; border-top: 1px solid #eee; padding-top: 15px; color: #aaaaaa; font-size: 12px;">
          هذه رسالة تلقائية، يرجى عدم الرد عليها.<br>
          منصة الخدمات الحكومية الرقمية — جميع الحقوق محفوظة
        </div>
        
      </div>
      
    </body>
    </html>
    """;
    }
}