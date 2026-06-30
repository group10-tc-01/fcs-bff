using Fcs.Bff.WebApi.Proxy;
using Microsoft.AspNetCore.Mvc;

namespace Fcs.Bff.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ProxyControllerBase : ControllerBase
{
    private readonly ProxyService _proxyService;

    protected ProxyControllerBase(ProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    protected async Task<IActionResult> ForwardAsync(
        string downstreamService,
        string downstreamPath,
        HttpMethod method,
        CancellationToken cancellationToken)
    {
        await _proxyService.ForwardAsync(HttpContext, downstreamService, downstreamPath, method, cancellationToken);
        return new EmptyResult();
    }

    protected async Task<IActionResult> ForwardJsonAsync<T>(
        string downstreamService,
        string downstreamPath,
        HttpMethod method,
        T body,
        CancellationToken cancellationToken)
    {
        await _proxyService.ForwardJsonAsync(HttpContext, downstreamService, downstreamPath, method, body, cancellationToken);
        return new EmptyResult();
    }
}
