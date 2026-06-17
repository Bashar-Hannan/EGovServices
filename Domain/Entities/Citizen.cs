namespace EGovServices.Domain.Entities;

public class Citizen
{
    public required string NationalNumber { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string FatherName { get; set; }
    public required string MotherName { get; set; }
    public required string Gender { get; set; }
    public required string Address { get; set; }
    public required DateOnly BirthDate { get; set; }
    public string? Email { get; set; }

    public ICollection<CitizenPhone> Phones { get; set; } = [];
    public User? User { get; set; }
}
