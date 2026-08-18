namespace EGovServices.Domain.Entities;

/// <summary>
/// فاتورة كهرباء وهمية — للتجربة والديمو.
/// مشابهة لـ TrafficViolation في المنطق:
/// استعلام برقم العداد → دفع من المحفظة.
/// </summary>
public class ElectricityBill
{
    public required Guid     Id             { get; set; }
    public required string   BillNumber     { get; set; }   // "ELEC-2026-000001"
    public required string   MeterNumber    { get; set; }   // رقم العداد
    public required string   CitizenNationalNumber { get; set; }  // FK → Citizens
    public required string   Month          { get; set; }   // "يونيو 2026"
    public required decimal  Amount         { get; set; }   // المبلغ المستحق
    public required string   Status         { get; set; }   // "Unpaid" | "Paid"
    public DateTime?         PaidAt         { get; set; }
    public Guid?             WalletTransactionId { get; set; }

    // Navigation
    public Citizen             Citizen            { get; set; } = null!;
    public WalletTransaction?  WalletTransaction  { get; set; }
}
