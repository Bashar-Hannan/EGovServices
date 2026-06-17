namespace EGovServices.Domain.Entities;

public class ServiceFieldOption
{
    public required Guid Id { get; set; }
    public required Guid ServiceFormFieldId { get; set; }
    public required string OptionValue { get; set; }
    public required string OptionLabel { get; set; }
    public required int DisplayOrder { get; set; }
    public required bool IsActive { get; set; }

    public ServiceFormField ServiceFormField { get; set; } = null!;
}
