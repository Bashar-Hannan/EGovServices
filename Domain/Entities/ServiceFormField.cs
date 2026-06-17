namespace EGovServices.Domain.Entities;

public class ServiceFormField
{
    public required Guid Id { get; set; }
    public required Guid GovernmentServiceId { get; set; }
    public required string FieldName { get; set; }
    public required string Label { get; set; }
    public required string FieldType { get; set; }
    public required bool IsRequired { get; set; }
    public required int DisplayOrder { get; set; }
    public string? ValidationRules { get; set; }
    public string? Placeholder { get; set; }
    public string? DefaultValue { get; set; }
    public string? HelpText { get; set; }
    public string? Metadata { get; set; }
    public required bool IsActive { get; set; }

    public GovernmentService GovernmentService { get; set; } = null!;
    public ICollection<ServiceFieldOption> Options { get; set; } = [];
}
