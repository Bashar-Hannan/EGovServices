namespace EGovServices.Application.Common.Interfaces;

/// <summary>
/// Marker interface — any Command that changes a ServiceRequest status must implement this.
/// The StatusTrackingBehavior uses it to auto-log AuditLog and send Notification.
/// </summary>
public interface IStatusChangingCommand
{
    Guid ServiceRequestId { get; }
}
