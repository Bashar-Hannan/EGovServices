namespace EGovServices.Domain.Entities;

public class FAQ
{
    public required Guid Id { get; set; }
    public required string Question { get; set; }
    public required string Answer { get; set; }
    public required string Category { get; set; }
    public required int DisplayOrder { get; set; }
    public required bool IsActive { get; set; }
}
