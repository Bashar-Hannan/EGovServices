namespace EGovServices.Domain.Entities;

public class RequestAuditLog
{
    public required Guid Id { get; set; }
    public required Guid ServiceRequestId { get; set; }
    public string? OldStatus { get; set; }
    public required string NewStatus { get; set; }
    public required string Action { get; set; }
    public string? Notes { get; set; }  // "Submitted","Approved","CertificateGenerated"
    public required DateTime CreatedAt { get; set; }

    // ✅ جديد — من غيّر الحالة (Admin أو Employee)
    public Guid? ChangedByUserId { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
