using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Citizen> Citizens { get; }
    DbSet<CitizenPhone> CitizenPhones { get; }
    DbSet<GovernmentEntity> GovernmentEntities { get; }
    DbSet<Branch> Branches { get; }
    DbSet<GovernmentService> GovernmentServices { get; }
    DbSet<ServiceFormField> ServiceFormFields { get; }
    DbSet<ServiceFieldOption> ServiceFieldOptions { get; }
    DbSet<ServiceRequest> ServiceRequests { get; }
    DbSet<AppointmentSlot> AppointmentSlots { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<RequestAuditLog> RequestAuditLogs { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<WalletTransaction> WalletTransactions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<FAQ> FAQs { get; }
    DbSet<CriminalRecord> CriminalRecords { get; }
    DbSet<OtpVerification> OtpVerifications { get; }
    DbSet<TrafficViolation> TrafficViolations { get; }
    DbSet<UploadedFile> UploadedFiles { get; }

    // ── NEW ──────────────────────────────────────────────────────────
    DbSet<ElectricityBill> ElectricityBills { get; }
    DbSet<MedicalRecord> MedicalRecords { get; }

    // ✅ جديد — جدول المستشفيات الحكومية المستقل
    DbSet<Hospital> Hospitals { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
