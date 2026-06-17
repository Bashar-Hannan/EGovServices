namespace EGovServices.Domain.Entities;

public class ServiceSlot
{
    public required Guid Id { get; set; }
    public required Guid GovernmentServiceId { get; set; }
    public required DateOnly Date { get; set; }
    public required TimeOnly StartTime { get; set; }
    public required TimeOnly EndTime { get; set; }
    public required int Capacity { get; set; }

    public GovernmentService GovernmentService { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = [];
}
