namespace EGovServices.Domain.Entities;

public class GovernmentEntity
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required bool IsActive { get; set; }

    // ? ÌÏíÏ — ãÓÇÑ ÇáÕæÑÉ ãËÇá: /images/ministries/interior.png
    public string? LogoUrl { get; set; }

    // Navigation
    public ICollection<GovernmentService> GovernmentServices { get; set; } = [];
    public ICollection<Branch> Branches { get; set; } = [];
}
