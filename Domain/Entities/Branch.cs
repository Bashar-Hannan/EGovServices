namespace EGovServices.Domain.Entities;

public class Branch
{
    public required Guid Id { get; set; }
    public required Guid GovernmentEntityId { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required string City { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? PhoneNumber { get; set; }
    public required bool IsActive { get; set; }

    public GovernmentEntity GovernmentEntity { get; set; } = null!;
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = [];
}
