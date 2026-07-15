
namespace EGovServices.Application.DTOs.ServiceSubmission;

public sealed record SubmitServiceRequest
{
    public required Guid ServiceId { get; init; }
    public required Dictionary<string, object> FormData { get; init; } = [];
}

public sealed record SubmitServiceResponse
{
    public required Guid RequestId { get; init; }
    public required string ReferenceNumber { get; init; }
    public required string Status { get; init; }
    public required DateTime SubmissionDate { get; init; }
    public required string Message { get; init; }
}