using EGovServices.Application.Features.Auth.Register;
using EGovServices.Application.Features.Auth.VerifyOtp;
using EGovServices.Application.Features.Auth.Login;
using FluentValidation;

namespace EGovServices.Application.Validators;

// ── Register Validator (unchanged) ──────────────────────────────
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.NationalNumber)
            .NotEmpty().WithMessage("رقم الهوية مطلوب")
            .Length(11).WithMessage("رقم الهوية يجب أن يكون 11 أرقام بالضبط")
            .Matches(@"^\d{11}$").WithMessage("رقم الهوية يجب أن يحتوي على أرقام فقط");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("رقم الجوال مطلوب")
            .Matches(@"^09\d{8}$").WithMessage("رقم الجوال يجب أن يبدأ بـ 09 ويتكون من 10 أرقام");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة")
            .MaximumLength(200).WithMessage("البريد الإلكتروني طويل جداً");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة")
            .MinimumLength(8).WithMessage("كلمة المرور يجب أن تكون 8 أحرف على الأقل")
            .MaximumLength(100).WithMessage("كلمة المرور طويلة جداً");
    }
}

// ── VerifyOtp Validator — only 2 fields now ──────────────────────
public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.NationalNumber)
            .NotEmpty().WithMessage("رقم الهوية مطلوب")
            .Length(11).WithMessage("رقم الهوية يجب أن يكون 11 أرقام")
            .Matches(@"^\d{11}$").WithMessage("رقم الهوية يجب أن يحتوي على أرقام فقط");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("رمز التحقق مطلوب")
            .Length(6).WithMessage("رمز التحقق يجب أن يكون 6 أرقام بالضبط")
            .Matches(@"^\d{6}$").WithMessage("رمز التحقق يجب أن يحتوي على أرقام فقط");
    }
}

// ── Login Validator (unchanged) ──────────────────────────────────
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.NationalNumber)
            .NotEmpty().WithMessage("رقم الهوية مطلوب")
            .Length(11).WithMessage("رقم الهوية يجب أن يكون 11 أرقام")
            .Matches(@"^\d{11}$").WithMessage("رقم الهوية يجب أن يحتوي على أرقام فقط");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة")
            .MinimumLength(8).WithMessage("كلمة المرور يجب أن تكون 8 أحرف على الأقل");
    }
}
