using Fcs.Bff.WebApi;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Text;

namespace Fcs.Bff.IntegratedTests;

public sealed class ProxyForwardingTests
{
    [Fact]
    public async Task TransparencyCampaigns_ForwardsQueryAndAuthHeaders()
    {
        using var downstream = new RecordingHttpMessageHandler();
        using var factory = CreateFactory(downstream);
        using var client = factory.CreateClient();
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/v1/transparency/campaigns?page=2&pageSize=5");
        requestMessage.Headers.Authorization = new("Bearer", "access-token");
        requestMessage.Headers.Add("Cookie", "auth=abc");
        requestMessage.Headers.Add("X-Correlation-ID", "corr-1");

        var response = await client.SendAsync(requestMessage);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        downstream.Requests.Should().ContainSingle();
        var request = downstream.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.PathAndQuery.Should().Be("/api/v1/transparency/campaigns?page=2&pageSize=5");
        request.GetValues("Authorization").Should().Contain("Bearer access-token");
        request.GetValues("Cookie").Should().Contain("auth=abc");
        request.GetValues("X-Correlation-ID").Should().Contain("corr-1");
    }

    [Fact]
    public async Task CompleteCampaign_ForwardsStatusPatchToCampaignApi()
    {
        using var downstream = new RecordingHttpMessageHandler();
        using var factory = CreateFactory(downstream);
        using var client = factory.CreateClient();
        var campaignId = Guid.NewGuid();

        var response = await client.PatchAsync($"/api/v1/campaigns/{campaignId}/complete", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        downstream.Requests.Should().ContainSingle();
        var request = downstream.Requests.Single();
        request.Method.Should().Be(HttpMethod.Patch);
        request.RequestUri!.PathAndQuery.Should().Be($"/api/v1/campaigns/{campaignId}/status");
        request.Body.Should().Contain("\"status\":\"Completed\"");
    }

    private static WebApplicationFactory<Program> CreateFactory(RecordingHttpMessageHandler handler)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IHttpClientFactory>();
                    services.AddSingleton<IHttpClientFactory>(_ => new StubHttpClientFactory(handler));
                });
            });
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://downstream")
            };
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri,
                request.Headers.ToDictionary(header => header.Key, header => header.Value.ToArray()),
                body));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"success\":true,\"data\":{\"items\":[],\"page\":1,\"pageSize\":10,\"totalCount\":0}}",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri? RequestUri,
        IReadOnlyDictionary<string, string[]> Headers,
        string Body)
    {
        public IEnumerable<string> GetValues(string headerName)
        {
            return Headers.TryGetValue(headerName, out var values) ? values : [];
        }
    }
}
