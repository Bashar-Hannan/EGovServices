using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Common.Interfaces;

/// <summary>
/// Updated IAppDbContext — added OtpVerifications.
/// Replace your existing 29_IAppDbContext.cs with this.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Citizen> Citizens { get; }
    DbSet<GovernmentEntity> GovernmentEntities { get; }
    DbSet<Branch> Branches { get; }
    DbSet<GovernmentService> GovernmentServices { get; }
    DbSet<ServiceFormField> ServiceFormFields { get; }
    DbSet<ServiceFieldOption> ServiceFieldOptions { get; }
    DbSet<ServiceRequest> ServiceRequests { get; }
    DbSet<ServiceSlot> ServiceSlots { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<RequestAuditLog> RequestAuditLogs { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<WalletTransaction> WalletTransactions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<FAQ> FAQs { get; }
    DbSet<CriminalRecord> CriminalRecords { get; }

    // ── NEW ──────────────────────────────────────────────────────────
    DbSet<OtpVerification> OtpVerifications { get; }
    DbSet<AppointmentSlot> AppointmentSlots { get; }
    DbSet<CitizenPhone> CitizenPhones { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
