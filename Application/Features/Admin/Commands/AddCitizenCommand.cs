using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Commands.AddCitizen;

// ── Command ───────────────────────────────────────────────────────────────────
public sealed record AddCitizenCommand : IRequest<Result<AddCitizenResponse>>
{
    // ── Required (تطابق الحقول غير الـ nullable على Citizen) ────────────
    public required string NationalNumber { get; init; }
    public required string FirstName { get; init; }
    public required string FatherName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly BirthDate { get; init; }
    public required string Email { get; init; }   // ← أُضيف: Citizen.Email غير nullable
    public required string Address { get; init; }   // ← أُضيف: Citizen.Address غير nullable

    // ── Optional (تطابق الحقول الـ nullable على Citizen) ─────────────────
    public string? PlaceOfBirth { get; init; }
    public string? MaritalStatus { get; init; }
    public string? MotherName { get; init; }   // ← أُزيلت MotherFullName (غير موجودة على Citizen)
    public string? Religion { get; init; }
    public string? Gender { get; init; }
    public string? RecordPlace { get; init; }
    public string? RecordNumber { get; init; }
}

// ── DTO ───────────────────────────────────────────────────────────────────────
public sealed record AddCitizenResponse(
    string NationalNumber,
    string FullName,
    string Message
);

// ── Validator ─────────────────────────────────────────────────────────────────
public sealed class AddCitizenValidator : AbstractValidator<AddCitizenCommand>
{
    public AddCitizenValidator()
    {
        RuleFor(x => x.NationalNumber)
            .NotEmpty()
            .Length(10)
            .Matches("^[0-9]{10}$")
            .WithMessage("رقم الهوية يجب أن يكون 10 أرقام بالضبط");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("الاسم الأول مطلوب ولا يتجاوز 50 حرف");

        RuleFor(x => x.FatherName)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("اسم الأب مطلوب ولا يتجاوز 50 حرف");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("اللقب مطلوب ولا يتجاوز 50 حرف");

        RuleFor(x => x.BirthDate)
            .NotEmpty()
            .Must(d => d < DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("تاريخ الميلاد يجب أن يكون في الماضي");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("بريد إلكتروني صحيح مطلوب");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("العنوان مطلوب");

        RuleFor(x => x.Gender)
            .Must(g => g is null || g == "ذكر" || g == "أنثى")
            .WithMessage("الجنس يجب أن يكون: ذكر أو أنثى");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class AddCitizenHandler(IAppDbContext context)
    : IRequestHandler<AddCitizenCommand, Result<AddCitizenResponse>>
{
    public async Task<Result<AddCitizenResponse>> Handle(
        AddCitizenCommand request, CancellationToken cancellationToken)
    {
        // ── 1. التحقق أن رقم الهوية غير مسجل مسبقاً ─────────────────
        var exists = await context.Citizens
            .AnyAsync(c => c.NationalNumber == request.NationalNumber, cancellationToken);

        if (exists)
            return Result<AddCitizenResponse>.Failure(
                $"رقم الهوية {request.NationalNumber} مسجل مسبقاً في السجلات المدنية");

        // ── 1.b. التحقق أن البريد الإلكتروني غير مستخدَم لمواطن آخر ────
        // ضروري لأن البريد يُستخدم لاحقاً كوسيلة تحقق عند التسجيل —
        // لو تكرر بين مواطنين، ينفتح احتمال ربط حساب بهوية غير صحيحة
        var emailTaken = await context.Citizens
            .AnyAsync(c => c.Email == request.Email, cancellationToken);

        if (emailTaken)
            return Result<AddCitizenResponse>.Failure(
                $"البريد الإلكتروني {request.Email} مستخدَم بالفعل لمواطن آخر في السجلات المدنية");

        // ── 2. إنشاء المواطن ──────────────────────────────────────────
        var citizen = new Domain.Entities.Citizen
        {
            NationalNumber = request.NationalNumber,
            FirstName = request.FirstName,
            FatherName = request.FatherName,
            LastName = request.LastName,
            BirthDate = request.BirthDate,
            Email = request.Email,
            Address = request.Address,
            PlaceOfBirth = request.PlaceOfBirth ?? string.Empty,
            MaritalStatus = request.MaritalStatus ?? string.Empty,
            MotherName = request.MotherName,
            Religion = request.Religion,
            Gender = request.Gender,
            RecordPlace = request.RecordPlace,
            RecordNumber = request.RecordNumber
        };

        await context.Citizens.AddAsync(citizen, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var fullName = $"{citizen.FirstName} {citizen.FatherName} {citizen.LastName}";

        return Result<AddCitizenResponse>.Success(new AddCitizenResponse(
            NationalNumber: citizen.NationalNumber,
            FullName: fullName,
            Message: $"تم إضافة المواطن {fullName} بنجاح — يمكنه الآن التسجيل عبر المنصة"
        ));
    }
}
