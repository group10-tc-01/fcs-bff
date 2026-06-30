using Fcs.Bff.WebApi.Proxy;
using Microsoft.AspNetCore.Mvc;

namespace Fcs.Bff.WebApi.Controllers;

public sealed class CampaignsController : ProxyControllerBase
{
    public CampaignsController(ProxyService proxyService) : base(proxyService)
    {
    }

    [HttpGet]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Campaign, "/api/v1/campaigns", HttpMethod.Get, cancellationToken);
    }

    [HttpGet("active")]
    public Task<IActionResult> GetActiveForDonors(CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Campaign, "/api/v1/transparency/campaigns", HttpMethod.Get, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Campaign, $"/api/v1/campaigns/{id}", HttpMethod.Get, cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Campaign, "/api/v1/campaigns", HttpMethod.Post, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Campaign, $"/api/v1/campaigns/{id}", HttpMethod.Put, cancellationToken);
    }

    [HttpPatch("{id:guid}/status")]
    public Task<IActionResult> UpdateStatus(Guid id, CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Campaign, $"/api/v1/campaigns/{id}/status", HttpMethod.Patch, cancellationToken);
    }

    [HttpPatch("{id:guid}/complete")]
    public Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        return ForwardJsonAsync(
            DownstreamServiceNames.Campaign,
            $"/api/v1/campaigns/{id}/status",
            HttpMethod.Patch,
            new UpdateCampaignStatusRequest("Completed"),
            cancellationToken);
    }

    [HttpPatch("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return ForwardJsonAsync(
            DownstreamServiceNames.Campaign,
            $"/api/v1/campaigns/{id}/status",
            HttpMethod.Patch,
            new UpdateCampaignStatusRequest("Canceled"),
            cancellationToken);
    }

    private sealed record UpdateCampaignStatusRequest(string Status);
}
