// =============================================================
// LOCATION:
// EGovServices.Application/
//   Features/
//     Profile/
//       Commands/
//         UpdatePhoneNumber/
//           UpdatePhoneNumberCommandValidator.cs
// =============================================================

using FluentValidation;

namespace EGovServices.Application.Features.Profile.Commands.UpdatePhoneNumber;

public sealed class UpdatePhoneNumberCommandValidator
    : AbstractValidator<UpdatePhoneNumberCommand>
{
    public UpdatePhoneNumberCommandValidator()
    {
        RuleFor(x => x.NewPhone)
            .NotEmpty().WithMessage("رقم الجوال مطلوب")
            .Matches(@"^09\d{8}$").WithMessage("رقم الجوال يجب أن يبدأ بـ 09 ويتكون من 10 أرقام");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("معرّف المستخدم مطلوب");
    }
}
