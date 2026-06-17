namespace EGovServices.Domain.Entities;

public class CitizenPhone
{
    public required Guid Id { get; set; }
    public required string Number { get; set; }
    public required string CitizenNationalNumber { get; set; }

    public Citizen Citizen { get; set; } = null!;
}
