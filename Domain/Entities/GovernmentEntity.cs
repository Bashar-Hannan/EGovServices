namespace EGovServices.Domain.Entities;

public class GovernmentEntity
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required bool IsActive { get; set; }

    public ICollection<Branch> Branches { get; set; } = [];
    public ICollection<GovernmentService> Services { get; set; } = [];
}
