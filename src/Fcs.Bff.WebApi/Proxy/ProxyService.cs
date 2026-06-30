using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Fcs.Bff.WebApi.Proxy;

public sealed class ProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProxyService> _logger;

    public ProxyService(IHttpClientFactory httpClientFactory, ILogger<ProxyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task ForwardAsync(
        HttpContext context,
        string downstreamService,
        string downstreamPath,
        HttpMethod method,
        CancellationToken cancellationToken)
    {
        await ForwardAsync(context, downstreamService, downstreamPath, method, overrideContent: null, cancellationToken);
    }

    public async Task ForwardJsonAsync<T>(
        HttpContext context,
        string downstreamService,
        string downstreamPath,
        HttpMethod method,
        T body,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        await ForwardAsync(context, downstreamService, downstreamPath, method, content, cancellationToken);
    }

    private async Task ForwardAsync(
        HttpContext context,
        string downstreamService,
        string downstreamPath,
        HttpMethod method,
        HttpContent? overrideContent,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(downstreamService);
        var target = downstreamPath + context.Request.QueryString;

        using var requestMessage = new HttpRequestMessage(method, target);
        CopyRequestHeaders(context, requestMessage);

        if (overrideContent is not null)
        {
            requestMessage.Content = overrideContent;
        }
        else if (CanHaveBody(method) && HasRequestBody(context.Request))
        {
            requestMessage.Content = new StreamContent(context.Request.Body);
            CopyRequestContentHeaders(context, requestMessage.Content.Headers);
        }

        _logger.LogInformation("Forwarding {Method} {Path} to {Service}{Target}", method, context.Request.Path, downstreamService, target);

        using var responseMessage = await client.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await CopyResponseAsync(context, responseMessage, cancellationToken);
    }

    private static void CopyRequestHeaders(HttpContext context, HttpRequestMessage requestMessage)
    {
        foreach (var header in context.Request.Headers)
        {
            if (HeaderForwarding.ShouldSkipHeader(header.Key) || HeaderForwarding.IsContentHeader(header.Key))
            {
                continue;
            }

            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    private static void CopyRequestContentHeaders(HttpContext context, HttpContentHeaders contentHeaders)
    {
        foreach (var header in context.Request.Headers)
        {
            if (!HeaderForwarding.IsContentHeader(header.Key) || HeaderForwarding.ShouldSkipHeader(header.Key))
            {
                continue;
            }

            contentHeaders.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    private static async Task CopyResponseAsync(HttpContext context, HttpResponseMessage responseMessage, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = (int)responseMessage.StatusCode;

        foreach (var header in responseMessage.Headers)
        {
            if (HeaderForwarding.ShouldSkipHeader(header.Key))
            {
                continue;
            }

            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in responseMessage.Content.Headers)
        {
            if (HeaderForwarding.ShouldSkipHeader(header.Key))
            {
                continue;
            }

            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        context.Response.Headers.Remove("transfer-encoding");

        if (responseMessage.Content.Headers.ContentLength == 0)
        {
            return;
        }

        await using var responseStream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);
        await responseStream.CopyToAsync(context.Response.Body, cancellationToken);
    }

    private static bool CanHaveBody(HttpMethod method)
    {
        return method != HttpMethod.Get &&
            method != HttpMethod.Head &&
            method != HttpMethod.Delete;
    }

    private static bool HasRequestBody(HttpRequest request)
    {
        return request.ContentLength is > 0 || string.Equals(request.Headers.TransferEncoding, "chunked", StringComparison.OrdinalIgnoreCase);
    }
}
