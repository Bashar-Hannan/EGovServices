using EGovServices.API.Services;
using EGovServices.Application.Common.Behaviors;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.Features.Jobs;
using EGovServices.Infrastructure.Persistence;
using EGovServices.Infrastructure.Service;
using EGovServices.Infrastructure.Service.Email;
using EGovServices.Infrastructure.Service;
using FluentValidation;
// أضفنا الـ usings الخاصة بـ Hangfire
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Text;
using System.Text;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

// ── 1. Database ───────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly("EGovServices.Infrastructure")));

builder.Services.AddScoped<IAppDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

// ── [جديد] تسجيل Hangfire في الـ Services ──────────────────────────
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString)); // يستخدم نفس قاعدة البيانات

builder.Services.AddHangfireServer();

// تسجيل الـ Job كـ Scoped Service لتتمكن من استخدامها
builder.Services.AddScoped<AppointmentReminderJob>();


// ── 2. MediatR + ValidationBehavior Pipeline ──────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(EGovServices.Application.Features.Auth.Login
            .LoginHandler).Assembly);

    // Register FluentValidation pipeline behavior
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));

});

// ── 3. FluentValidation — scans Application assembly ─────────────
builder.Services.AddValidatorsFromAssembly(
    typeof(EGovServices.Application.Validators
        .LoginCommandValidator).Assembly);

// ── 4. Email Service ──────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, MailKitEmailService>();

// ── 5. Other Services ─────────────────────────────────────────────
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IVerificationTokenService, VerificationTokenService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddHttpContextAccessor();

// ── 6. Controllers + Swagger ──────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    // سياسة خاصة بـ Verification Endpoint
    // تسمح بـ 10 طلبات كل دقيقة لكل IP
    options.AddFixedWindowLimiter("VerificationPolicy", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueLimit = 0;     // لا انتظار — رفض مباشر
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // رد موحّد عند تجاوز الحد
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsync(
            """{"message": "تجاوزت الحد المسموح من الطلبات. حاول مجدداً بعد دقيقة."}""",
            cancellationToken);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "E-Government Services API",
        Version = "v1"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "أدخل: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        Array.Empty<string>()
    }});
});

// ── 7. JWT Authentication ─────────────────────────────────────────
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// ── 8. CORS ───────────────────────────────────────────────────────
builder.Services.AddCors(o => o.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ─────────────────────────────────────────────────────────────────
var app = builder.Build();
app.UseStaticFiles();
// ─────────────────────────────────────────────────────────────────

// ── تعديل معالجة الأخطاء لإرجاع رسائل مبسطة ──────────────────────
// ── معالجة الأخطاء الذكية لإرسال رسائل نظيفة ومبسطة للموبايل ──────────────────────
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var errorFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (errorFeature != null)
        {
            context.Response.ContentType = "application/json";
            var exception = errorFeature.Error;

            // افتراضياً نعتبره خطأ سيرفر داخلي 500 ورسالة عامة لحماية النظام
            var statusCode = StatusCodes.Status500InternalServerError;
            var message = "حدث خطأ داخلي في الخادم.";

            // 1. إذا كان الخطأ قادماً من FluentValidation
            if (exception is FluentValidation.ValidationException validationException)
            {
                statusCode = StatusCodes.Status400BadRequest;

                // جلب نص الرسالة العربية النقية فقط بدون أي إضافات إنجليزية
                message = validationException.Errors.FirstOrDefault()?.ErrorMessage
                          ?? "بيانات المدخلات غير صالحة.";
            }
            // 2. إذا كان الخطأ قادماً كـ BadHttpRequestException مباشر
            else if (exception is BadHttpRequestException)
            {
                statusCode = StatusCodes.Status400BadRequest;
                message = exception.Message;
            }

            // تعيين كود الحالة المناسب (400 أو 500)
            context.Response.StatusCode = statusCode;

            // إرسال الرد المبسط النهائي
            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    message = message
                }));
        }
    });
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Gov Services API V1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseStaticFiles();

// انتبه: يجب وضع لوحة تحكم Hangfire بعد UseAuthentication و UseAuthorization 
// لكي يتعرف النظام على الـ Roles (مثل Admin) قبل دخول الفلتر
app.UseAuthentication();
app.UseAuthorization();

// ✅ Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // هنا تحدد اسم المستخدم وكلمة المرور
    Authorization = [new HangfireBasicAuthFilter("admin", "Admin@12345!")]
});

// ✅ مطلوب في الإصدارات الحديثة
app.MapHangfireDashboard();
app.UseRateLimiter();
app.MapControllers();

// ✅ Migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ✅ RecurringJob — مرة واحدة فقط بدون scope
RecurringJob.AddOrUpdate<AppointmentReminderJob>(
    "appointment-reminders",
    job => job.SendRemindersAsync(),
    "0 8 * * *");

app.Run();


// ═══════════════════════════════════════════════════════════════════════════
// فلتر المصادقة الخاص بلوحة Hangfire
// ═══════════════════════════════════════════════════════════════════════════
public class HangfireBasicAuthFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    public HangfireBasicAuthFilter(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var header = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(header))
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(header);
            if (!"Basic".Equals(authHeader.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                SetChallengeResponse(httpContext);
                return false;
            }

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? string.Empty);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

            if (credentials.Length == 2 && credentials[0] == _username && credentials[1] == _password)
            {
                return true; // ✅ تم التحقق بنجاح
            }
        }
        catch
        {
            // تجاهل الخطأ في حال كانت البيانات مشوهة
        }

        SetChallengeResponse(httpContext);
        return false;
    }

    private void SetChallengeResponse(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
    }
}
// ═══════════════════════════════════════════════════════════════════════════
// نضع الكلاسات والفلتر في نهاية الملف تماماً لكي لا تعترض الكود التنفيذي العلوي
// ═══════════════════════════════════════════════════════════════════════════
//public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
//{
//    public bool Authorize(DashboardContext context)
//    {
//        var httpContext = context.GetHttpContext();

//        // يسمح فقط للمهندسين المسجلين دخولاً بدور Admin
//        return httpContext.User.Identity?.IsAuthenticated == true &&
//               httpContext.User.IsInRole("Admin");
//    }
//}
//public class AllowAllDashboardFilter : IDashboardAuthorizationFilter
//{
//    public bool Authorize(DashboardContext context) => true;
//}