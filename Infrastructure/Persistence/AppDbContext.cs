using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EGovServices.Infrastructure.Persistence;

/// <summary>
/// Updated AppDbContext — added OtpVerifications DbSet.
/// Replace your existing 27_AppDbContext.cs with this.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Citizen> Citizens => Set<Citizen>();
    public DbSet<CitizenPhone> CitizenPhones => Set<CitizenPhone>();
    public DbSet<GovernmentEntity> GovernmentEntities => Set<GovernmentEntity>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<GovernmentService> GovernmentServices => Set<GovernmentService>();
    public DbSet<ServiceFormField> ServiceFormFields => Set<ServiceFormField>();
    public DbSet<ServiceFieldOption> ServiceFieldOptions => Set<ServiceFieldOption>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceSlot> ServiceSlots => Set<ServiceSlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<RequestAuditLog> RequestAuditLogs => Set<RequestAuditLog>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<FAQ> FAQs => Set<FAQ>();
    public DbSet<CriminalRecord> CriminalRecords => Set<CriminalRecord>();

    // ── NEW ──────────────────────────────────────────────────────────
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        //#if DEBUG

        //#endif
    }
}

