using Fcs.Bff.WebApi.Proxy;
using Microsoft.AspNetCore.Mvc;

namespace Fcs.Bff.WebApi.Controllers;

[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ProxyControllerBase
{
    public AuthController(ProxyService proxyService) : base(proxyService)
    {
    }

    [HttpPost("register/donor")]
    public Task<IActionResult> RegisterDonor(CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Identity, "/api/v1/auth/register/donor", HttpMethod.Post, cancellationToken);
    }

    [HttpPost("login")]
    public Task<IActionResult> Login(CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Identity, "/api/v1/auth/login", HttpMethod.Post, cancellationToken);
    }

    [HttpPost("refresh")]
    public Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Identity, "/api/v1/auth/refresh", HttpMethod.Post, cancellationToken);
    }

    [HttpPost("logout")]
    public Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        return ForwardAsync(DownstreamServiceNames.Identity, "/api/v1/auth/logout", HttpMethod.Post, cancellationToken);
    }
}
