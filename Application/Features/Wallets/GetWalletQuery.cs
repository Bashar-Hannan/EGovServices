using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EGovServices.Domain.Entities;

namespace EGovServices.Application.Features.Wallets;

// ── Query ─────────────────────────────────────────────────────────────────────
public sealed record GetWalletQuery(Guid UserId)
    : IRequest<Result<WalletDetailsDto>>;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record WalletDetailsDto(
    Guid                      WalletId,
    decimal                   Balance,
    string                    Currency,
    string                    LastUpdated,
    List<WalletTransactionDto> Transactions
);

public sealed record WalletTransactionDto(
    Guid    Id,
    decimal Amount,
    string  TransactionType,
    string  TransactionTypeLabel,
    string  Description,
    string  CreatedAt
);

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetWalletHandler(IAppDbContext context)
    : IRequestHandler<GetWalletQuery, Result<WalletDetailsDto>>
{
    public async Task<Result<WalletDetailsDto>> Handle(
        GetWalletQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await context.Wallets
            .AsNoTracking()
            .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt))
            .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

        if (wallet is null)
            return Result<WalletDetailsDto>.Failure("المحفظة غير موجودة");

        var transactions = wallet.Transactions.Select(t => new WalletTransactionDto(
            Id:                   t.Id,
            Amount:               t.Amount,
            TransactionType:      t.TransactionType,
            TransactionTypeLabel: GetTypeLabel(t.TransactionType),
            Description:          t.Description ?? "—",
            CreatedAt:            t.CreatedAt.ToString("dd/MM/yyyy HH:mm")
        )).ToList();

        return Result<WalletDetailsDto>.Success(new WalletDetailsDto(
            WalletId:    wallet.Id,
            Balance:     wallet.Balance,
            Currency:    "SYP",
            LastUpdated: transactions.Count > 0
                            ? wallet.Transactions.Max(t => t.CreatedAt).ToString("dd/MM/yyyy HH:mm")
                            : "—",
            Transactions: transactions
        ));
    }

    private static string GetTypeLabel(string type) => type switch
    {
        "Deposit"            => "إيداع",
        "Withdrawal"         => "سحب",
        "ServiceFeePayment"  => "دفع رسوم خدمة",
        "ViolationPayment"   => "دفع مخالفة مرورية",
        _                    => type
    };
}
