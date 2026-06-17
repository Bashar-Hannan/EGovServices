namespace EGovServices.Domain.Entities;

/// <summary>
/// Represents a criminal record in the simulated government database.
/// Each record is linked to a citizen via their NationalNumber.
/// </summary>
public class CriminalRecord
{
    /// <summary>Unique identifier for the record</summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// NationalNumber of the citizen this record belongs to.
    /// FK → Citizens.NationalNumber
    /// </summary>
    public required string CitizenNationalNumber { get; set; }

    /// <summary>
    /// Description of the crime committed.
    /// Example: "سرقة بسيطة", "مخالفة مرورية"
    /// </summary>
    public required string CrimeDescription { get; set; }

    /// <summary>
    /// Date the judgment was issued by the court.
    /// </summary>
    public required DateOnly JudgmentDate { get; set; }

    /// <summary>
    /// Whether this record is currently active (unsettled sentence).
    /// true  = active criminal record (pending or ongoing sentence)
    /// false = record exists but sentence completed / pardoned
    /// </summary>
    public required bool IsActive { get; set; }

    // Navigation
    public Citizen Citizen { get; set; } = null!;
}
