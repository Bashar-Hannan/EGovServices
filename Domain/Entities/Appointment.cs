namespace EGovServices.Domain.Entities;

public class Appointment
{
    public required Guid Id { get; set; }
    public required Guid ServiceRequestId { get; set; }
    public required Guid ServiceSlotId { get; set; }
    public required string Status { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = null!;
    public ServiceSlot ServiceSlot { get; set; } = null!;
}
