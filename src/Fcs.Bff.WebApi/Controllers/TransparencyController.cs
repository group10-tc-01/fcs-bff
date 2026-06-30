using Fcs.Bff.WebApi.Proxy;
using Microsoft.AspNetCore.Mvc;

namespace Fcs.Bff.WebApi.Controllers;

[Route("api/v{version:apiVersion}/transparency/campaigns")]
public sealed class TransparencyController : ProxyControllerBase
{
    public TransparencyController(ProxyService proxyService) : base(proxyService)
    {
    }

    [HttpGet]
    public Task<IActionResult> GetCampaigns(CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Campaign, "/api/v1/transparency/campaigns", HttpMethod.Get, cancellationToken);
    }
}
