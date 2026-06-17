namespace EGovServices.Domain.Entities;

public class Notification
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string NotificationType { get; set; }
    public required bool IsRead { get; set; }
    public required DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
