using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Queries;

public sealed record CitizenDto
{
    public required string NationalNumber { get; init; }
    public required string FullName       { get; init; }
    public required string BirthDate      { get; init; }
    public string?         PlaceOfBirth   { get; init; }
    public string?         Gender         { get; init; }
    public string?         MaritalStatus  { get; init; }
    public string?         Religion       { get; init; }
    public string?         MotherName     { get; init; }
    public string?         RecordPlace    { get; init; }
    public string?         RecordNumber   { get; init; }
    public bool            HasAccount     { get; init; }
}

public sealed record GetCitizensQuery : IRequest<Result<List<CitizenDto>>>;

public sealed class GetCitizensHandler(IAppDbContext context)
    : IRequestHandler<GetCitizensQuery, Result<List<CitizenDto>>>
{
    public async Task<Result<List<CitizenDto>>> Handle(
        GetCitizensQuery request, CancellationToken cancellationToken)
    {
        var citizens = await context.Citizens
            .AsNoTracking()
            .Include(c => c.User)
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Select(c => new CitizenDto
            {
                NationalNumber = c.NationalNumber,
                FullName       = c.FirstName + " " + c.FatherName + " " + c.LastName,
                BirthDate      = c.BirthDate.ToString("dd/MM/yyyy"),
                PlaceOfBirth   = c.PlaceOfBirth,
                Gender         = c.Gender,
                MaritalStatus  = c.MaritalStatus,
                Religion       = c.Religion,
                MotherName     = c.MotherName,
                RecordPlace    = c.RecordPlace,
                RecordNumber   = c.RecordNumber,
                HasAccount     = c.User != null
            })
            .ToListAsync(cancellationToken);

        return Result<List<CitizenDto>>.Success(citizens);
    }
}

public sealed record GetCitizenQuery(string NationalNumber)
    : IRequest<Result<CitizenDto>>;

public sealed class GetCitizenHandler(IAppDbContext context)
    : IRequestHandler<GetCitizenQuery, Result<CitizenDto>>
{
    public async Task<Result<CitizenDto>> Handle(
        GetCitizenQuery request, CancellationToken cancellationToken)
    {
        var citizen = await context.Citizens
            .AsNoTracking()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.NationalNumber == request.NationalNumber, cancellationToken);

        if (citizen is null)
            return Result<CitizenDto>.Failure($"لا يوجد مواطن برقم الهوية {request.NationalNumber}");

        return Result<CitizenDto>.Success(new CitizenDto
        {
            NationalNumber = citizen.NationalNumber,
            FullName       = $"{citizen.FirstName} {citizen.FatherName} {citizen.LastName}",
            BirthDate      = citizen.BirthDate.ToString("dd/MM/yyyy"),
            PlaceOfBirth   = citizen.PlaceOfBirth,
            Gender         = citizen.Gender,
            MaritalStatus  = citizen.MaritalStatus,
            Religion       = citizen.Religion,
            MotherName     = citizen.MotherName,
            RecordPlace    = citizen.RecordPlace,
            RecordNumber   = citizen.RecordNumber,
            HasAccount     = citizen.User != null
        });
    }
}