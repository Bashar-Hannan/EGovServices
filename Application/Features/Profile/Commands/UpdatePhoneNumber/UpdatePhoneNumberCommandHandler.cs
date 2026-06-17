// =============================================================
// LOCATION:
// EGovServices.Application/
//   Features/
//     Profile/
//       Commands/
//         UpdatePhoneNumber/
//           UpdatePhoneNumberCommandHandler.cs
//
// NOTE:
//   يستخدم IAppDbContext وليس AppDbContext مباشرةً
//   حتى لا تعتمد طبقة Application على Infrastructure
// =============================================================

using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Profile.Commands.UpdatePhoneNumber;

public sealed class UpdatePhoneNumberCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdatePhoneNumberCommand, Result>
{
    public async Task<Result> Handle(
        UpdatePhoneNumberCommand request,
        CancellationToken cancellationToken)
    {
        // 1. التحقق من وجود المستخدم
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure("المستخدم غير موجود");

        // 2. التحقق أن الرقم الجديد ليس مستخدماً عند شخص آخر
        var phoneExists = await context.Users
            .AnyAsync(u => u.PhoneNumber == request.NewPhone
                        && u.Id != request.UserId, cancellationToken);

        if (phoneExists)
            return Result.Failure("رقم الجوال مستخدم مسبقاً");

        var isOwnedByCitizen = await context.CitizenPhones
            .AnyAsync(cp => cp.CitizenNationalNumber == user.NationalNumber
                         && cp.Number == request.NewPhone, cancellationToken);

        if (!isOwnedByCitizen)
            return Result.Failure("عذراً، رقم الجوال الجديد ليس مسجلاً باسمك في السجلات الرسمية");

        // 3. تحديث الرقم وحفظه
        user.PhoneNumber = request.NewPhone;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
