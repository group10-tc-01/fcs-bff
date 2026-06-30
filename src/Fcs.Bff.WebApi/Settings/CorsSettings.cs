using System.ComponentModel.DataAnnotations;

namespace Fcs.Bff.WebApi.Settings;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    [Required]
    [MinLength(1)]
    public string[] AllowedOrigins { get; init; } = ["http://localhost:4200"];
}
