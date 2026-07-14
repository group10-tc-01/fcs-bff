using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi;

namespace Fcs.Bff.WebApi.Swagger;

[ExcludeFromCodeCoverage]
public static class SwaggerDependencyInjection
{
    public static IServiceCollection AddBffSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Fcs.Bff API",
                Version = "v1.0",
                Description = BuildDeploymentDescription(configuration)
            });
        });

        return services;
    }

    private static string BuildDeploymentDescription(IConfiguration configuration)
    {
        var deployedAt = configuration["Deployment:DeployedAt"] ?? "não informado";
        var sourceSha = configuration["Deployment:SourceSha"] ?? "não informado";
        var image = configuration["Deployment:Image"] ?? "não informado";

        return $"""
            Backend for Frontend da plataforma Conexão Solidária.

            ### Implantação em execução

            - **Data/hora (UTC):** {deployedAt}
            - **Commit:** `{sourceSha}`
            - **Imagem:** `{image}`
            """;
    }
}
