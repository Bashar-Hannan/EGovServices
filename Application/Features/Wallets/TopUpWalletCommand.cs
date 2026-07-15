using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Wallets;

// ── Command ───────────────────────────────────────────────────────────────────
/// <summary>
/// شحن رصيد وهمي للمحفظة — لأغراض الديمو والاختبار فقط.
/// لا يوجد أي تكامل حقيقي مع بطاقة ائتمان أو بنك.
/// </summary>
public sealed record TopUpWalletCommand(Guid UserId, decimal Amount)
    : IRequest<Result<TopUpWalletResponse>>;

// ── DTO ───────────────────────────────────────────────────────────────────────
public sealed record TopUpWalletResponse(
    decimal PreviousBalance,
    decimal AmountAdded,
    decimal NewBalance
);

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class TopUpWalletHandler(IAppDbContext context)
    : IRequestHandler<TopUpWalletCommand, Result<TopUpWalletResponse>>
{
    public async Task<Result<TopUpWalletResponse>> Handle(
        TopUpWalletCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return Result<TopUpWalletResponse>.Failure("يجب أن يكون المبلغ أكبر من صفر");

        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

        if (wallet is null)
            return Result<TopUpWalletResponse>.Failure("المحفظة غير موجودة لهذا المستخدم");

        var previousBalance = wallet.Balance;
        wallet.Balance += request.Amount;

        context.WalletTransactions.Add(new WalletTransaction
        {
            Id              = Guid.NewGuid(),
            WalletId        = wallet.Id,
            Amount          = request.Amount,
            TransactionType = "Deposit",
            Description     = "إيداع",
            CreatedAt       = DateTime.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);

        return Result<TopUpWalletResponse>.Success(new TopUpWalletResponse(
            PreviousBalance: previousBalance,
            AmountAdded:     request.Amount,
            NewBalance:      wallet.Balance
        ));
    }
}
