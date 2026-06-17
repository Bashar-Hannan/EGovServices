// ============================================================
// File: Application/DTOs/FormSchema/FormSchemaDtos.cs
// ============================================================
namespace EGovServices.Application.DTOs.FormSchema;

public sealed record FormSchemaResponse
{
    public required Guid ServiceId { get; init; }
    public required string ServiceName { get; init; }
    public required string Description { get; init; }
    public string? Requirements { get; init; }
    public required decimal ServiceFee { get; init; }
    public required List<FormFieldDto> Fields { get; init; } = [];
}

public sealed record FormFieldDto
{
    public required string Name { get; init; }
    public required string Label { get; init; }
    public required string Type { get; init; }
    public required bool Required { get; init; }
    public required int Order { get; init; }
    public string? Placeholder { get; init; }
    public string? DefaultValue { get; init; }
    public string? HelpText { get; init; }
    public ValidationRulesDto? Validation { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public List<FieldOptionDto>? Options { get; init; }
}

public sealed record ValidationRulesDto
{
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public int? Length { get; init; }
    public string? Pattern { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public long? MaxSize { get; init; }
    public string[]? AllowedTypes { get; init; }
    public string? MinDate { get; init; }
    public string? MaxDate { get; init; }
    public string? CustomMessage { get; init; }
}

public sealed record FieldOptionDto
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}



