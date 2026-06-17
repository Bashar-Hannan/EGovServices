namespace EGovServices.Domain.Entities;

public class Wallet
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required decimal Balance { get; set; }
    public required string Currency { get; set; }
    public required DateTime LastUpdated { get; set; }
    public required bool IsLocked { get; set; }

    public User User { get; set; } = null!;
    public ICollection<WalletTransaction> Transactions { get; set; } = [];
}
