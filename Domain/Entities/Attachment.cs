namespace EGovServices.Domain.Entities;

public class Attachment
{
    public required Guid Id { get; set; }
    public required Guid ServiceRequestId { get; set; }
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public required string ContentType { get; set; }
    public required string FileType { get; set; }
    public required long FileSizeBytes { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = null!;
}
