namespace EGovServices.Domain.Entities;

public class Citizen
{
    public required string NationalNumber { get; set; }
    public required string FirstName { get; set; }
    public required string FatherName { get; set; }
    public required string LastName { get; set; }
    public  DateOnly BirthDate { get; set; }
    public  string PlaceOfBirth { get; set; }
    public string MaritalStatus { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }

    // ?? NEW — ÍŞæá ÇáŞíÏ ÇáãÏäí (nullable áÃä ÇáÈíÇäÇÊ ÇáŞÏíãÉ ãÇ ÚäÏåÇ åĞå ÇáÍŞæá) ??
    public string? MotherName { get; set; }     // ÇÓã ÇáÃã æäÓÈÊåÇ
    public string? Religion { get; set; }           // ÇáÏíä
    public string? Gender { get; set; }             // ÇáÌäÓ
    public string? RecordPlace { get; set; }        // ãÍá ÇáŞíÏ
    public string? RecordNumber { get; set; }       // ÑŞã ÇáŞíÏ

    // Navigation
    public User? User { get; set; }
    public ICollection<CriminalRecord> CriminalRecords { get; set; } = [];
}

