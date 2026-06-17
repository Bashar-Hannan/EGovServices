namespace EGovServices.Domain.Entities;

public class WalletTransaction
{
    public required Guid Id { get; set; }
    public required Guid WalletId { get; set; }
    public required decimal Amount { get; set; }
    public required string TransactionType { get; set; }
    public string? Description { get; set; }
    public string? ReferenceId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public Guid? ServiceRequestId { get; set; }

    public Wallet Wallet { get; set; } = null!;
    public ServiceRequest? ServiceRequest { get; set; }
}
