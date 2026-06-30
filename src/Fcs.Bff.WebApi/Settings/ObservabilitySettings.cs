using System.ComponentModel.DataAnnotations;

namespace Fcs.Bff.WebApi.Settings;

public sealed class ObservabilitySettings
{
    public const string SectionName = "Observability";

    [Required]
    public string ServiceName { get; init; } = "Fcs.Bff";

    public bool EnableOtlpExporter { get; init; }

    public string OtlpEndpoint { get; init; } = string.Empty;

    public string OtlpAuthHeader { get; init; } = string.Empty;
}
