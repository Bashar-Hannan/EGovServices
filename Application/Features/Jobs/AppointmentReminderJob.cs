using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EGovServices.Application.Features.Jobs;

/// <summary>
/// Hangfire Recurring Job — يعمل كل يوم الساعة 8 صباحاً
/// يبحث عن المواعيد المؤكدة التي موعدها غداً ويرسل تذكيراً
/// للمواطن والموظف المسؤول
/// </summary>
public class AppointmentReminderJob(
    IAppDbContext context,
    ILogger<AppointmentReminderJob> logger)
{
    public async Task SendRemindersAsync()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        logger.LogInformation("AppointmentReminderJob: البحث عن مواعيد {Date}", tomorrow);

        // كل الطلبات المؤكدة التي موعدها غداً
        var upcomingRequests = await context.ServiceRequests
            .AsNoTracking()
            .Include(r => r.AppointmentSlot)
            .Include(r => r.User)
            .Include(r => r.GovernmentService)
            .Where(r =>
                r.AppointmentSlot != null &&
                r.AppointmentSlot.SlotDate == tomorrow &&
                r.Status == RequestStatus.AppointmentConfirmed &&
                !r.IsDeleted)
            .ToListAsync();

        if (!upcomingRequests.Any())
        {
            logger.LogInformation("AppointmentReminderJob: لا توجد مواعيد غداً");
            return;
        }

        logger.LogInformation(
            "AppointmentReminderJob: وُجد {Count} موعد غداً", upcomingRequests.Count);

        var notifications = new List<Notification>();

        foreach (var request in upcomingRequests)
        {
            var slot    = request.AppointmentSlot!;
            var service = request.GovernmentService;

            // ── إشعار المواطن ──────────────────────────────────────────────
            notifications.Add(new Notification
            {
                Id               = Guid.NewGuid(),
                UserId           = request.UserId,
                Title            = "تذكير: لديك موعد غداً",
                Message          = $"تذكير بموعدك غداً {slot.SlotDate} " +
                                   $"الساعة {slot.StartTime:hh\\:mm} " +
                                   $"لخدمة \"{service.Name}\". " +
                                   $"رقم الطلب: {request.ReferenceNumber}. " +
                                   $"يرجى الحضور قبل الموعد بـ 10 دقائق.",
                NotificationType = "Reminder",
                IsRead           = false,
                CreatedAt        = DateTime.UtcNow
            });

            // ── إشعار الموظف المسؤول عن الخدمة ────────────────────────────
            // نجلب كل الموظفين المرتبطين بهذه الخدمة
            var employees = await context.Users
                .AsNoTracking()
                .Where(u => u.Role == "Employee" && u.IsActive)
                .ToListAsync();

            foreach (var employee in employees)
            {
                notifications.Add(new Notification
                {
                    Id               = Guid.NewGuid(),
                    UserId           = employee.Id,
                    Title            = "تذكير: موعد غداً",
                    Message          = $"يوجد موعد غداً {slot.SlotDate} " +
                                       $"الساعة {slot.StartTime:hh\\:mm} " +
                                       $"لخدمة \"{service.Name}\". " +
                                       $"المواطن: {request.User.NationalNumber} — " +
                                       $"رقم الطلب: {request.ReferenceNumber}",
                    NotificationType = "Reminder",
                    IsRead           = false,
                    CreatedAt        = DateTime.UtcNow
                });
            }
        }

        await context.Notifications.AddRangeAsync(notifications);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "AppointmentReminderJob: تم إرسال {Count} إشعار", notifications.Count);
    }
}
