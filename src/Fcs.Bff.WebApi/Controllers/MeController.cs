using Fcs.Bff.WebApi.Proxy;
using Microsoft.AspNetCore.Mvc;

namespace Fcs.Bff.WebApi.Controllers;

[Route("api/v{version:apiVersion}/me")]
public sealed class MeController : ProxyControllerBase
{
    public MeController(ProxyService proxyService) : base(proxyService)
    {
    }

    [HttpGet]
    public Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Identity, "/api/v1/me", HttpMethod.Get, cancellationToken);
    }
}
