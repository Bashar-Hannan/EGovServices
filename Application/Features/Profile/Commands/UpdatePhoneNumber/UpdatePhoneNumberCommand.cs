// =============================================================
// LOCATION:
// EGovServices.Application/
//   Features/
//     Profile/
//       Commands/
//         UpdatePhoneNumber/
//           UpdatePhoneNumberCommand.cs
// =============================================================

using EGovServices.Application.Common;
using MediatR;

namespace EGovServices.Application.Features.Profile.Commands.UpdatePhoneNumber;

public sealed record UpdatePhoneNumberCommand : IRequest<Result>
{
    public required Guid UserId    { get; init; }
    public required string NewPhone { get; init; }
}
