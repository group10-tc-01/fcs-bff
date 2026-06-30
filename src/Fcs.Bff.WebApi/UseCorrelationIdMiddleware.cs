namespace Fcs.Bff.WebApi;

public static class UseCorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString();

            context.Request.Headers[HeaderName] = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            await next();
        });
    }
}
