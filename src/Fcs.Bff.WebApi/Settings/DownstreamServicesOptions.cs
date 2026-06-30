using System.ComponentModel.DataAnnotations;

namespace Fcs.Bff.WebApi.Settings;

public sealed class DownstreamServicesOptions
{
    public const string SectionName = "DownstreamServices";

    [Required]
    [Url]
    public string IdentityBaseUrl { get; init; } = string.Empty;

    [Required]
    [Url]
    public string CampaignBaseUrl { get; init; } = string.Empty;

    [Required]
    [Url]
    public string DonationsBaseUrl { get; init; } = string.Empty;
}
