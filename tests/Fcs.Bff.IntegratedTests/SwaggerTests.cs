using System.Text.Json;
using Fcs.Bff.WebApi;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Fcs.Bff.IntegratedTests;

public sealed class SwaggerTests
{
    [Fact]
    public async Task GetSwaggerDocument_WhenDeploymentMetadataIsConfigured_IncludesCurrentDeployment()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Deployment:DeployedAt"] = "2026-07-14T04:00:00Z",
                    ["Deployment:SourceSha"] = "abc123",
                    ["Deployment:Image"] = "ghcr.io/group10-tc-01/fcs-bff:abc123"
                })));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.IsSuccessStatusCode.Should().BeTrue();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var description = document.RootElement
            .GetProperty("info")
            .GetProperty("description")
            .GetString();

        description.Should().Contain("2026-07-14T04:00:00Z");
        description.Should().Contain("abc123");
        description.Should().Contain("ghcr.io/group10-tc-01/fcs-bff:abc123");
    }
}
