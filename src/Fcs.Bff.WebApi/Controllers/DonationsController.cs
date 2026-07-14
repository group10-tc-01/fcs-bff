using Fcs.Bff.WebApi.Proxy;
using Microsoft.AspNetCore.Mvc;

namespace Fcs.Bff.WebApi.Controllers;

public sealed class DonationsController : ProxyControllerBase
{
    public DonationsController(ProxyService proxyService) : base(proxyService)
    {
    }

    [HttpGet]
    public Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Donations, "/api/v1/donations", HttpMethod.Get, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Donations, $"/api/v1/donations/{id}", HttpMethod.Get, cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Donations, "/api/v1/donations", HttpMethod.Post, cancellationToken);
    }
}
