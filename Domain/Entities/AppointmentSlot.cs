namespace EGovServices.Domain.Entities;

public class AppointmentSlot
{
    public required Guid Id { get; set; }
    public required Guid GovernmentServiceId { get; set; }
    public required Guid CreatedByAdminId { get; set; }

    public required DateOnly SlotDate { get; set; }     // تاريخ الموعد
    public required TimeOnly StartTime { get; set; }    // 09:00
    public required TimeOnly EndTime { get; set; }      // 10:00

    public required int TotalSeats { get; set; }        // المقاعد الكلية يحددها Admin
    public int BookedSeats { get; set; } = 0;           // تزيد عند كل حجز ناجح

    public required bool IsActive { get; set; } = true;
    public required DateTime CreatedAt { get; set; }

    /// <summary>يستخدمه النظام لمنع الحجز عند الامتلاء</summary>
    public bool IsFull => BookedSeats >= TotalSeats;

    /// <summary>المقاعد المتبقية للعرض في الواجهة</summary>
    public int AvailableSeats => TotalSeats - BookedSeats;

    // Navigation
    public GovernmentService GovernmentService { get; set; } = null!;
    public User CreatedByAdmin { get; set; } = null!;
    public ICollection<ServiceRequest> Requests { get; set; } = [];
}
