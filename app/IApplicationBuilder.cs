using System.Text;

public interface IApplicationBuilder
{
    List<Func<RequestDelegate, RequestDelegate>> Middlewares { get; }
    IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);
    IApplicationBuilder Use(Func<HttpContext, RequestDelegate, Task> middleware);
    public RequestDelegate Build();
}

public static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRouting(this IApplicationBuilder builder)
    {
        builder.Use((context, next) =>
        {
            if (context.Request.Url?.AbsolutePath is {} targetPath
                && builder is IEndpointRouteBuilder app
                && app.Endpoints.TryGetValue(targetPath, out var endpoint)
                && endpoint.Method.Equals(context.Request.HttpMethod, StringComparison.OrdinalIgnoreCase))
            {
                context.FeatureCollection.Add("Endpoint", endpoint);
            }

            return next(context);
        });

        return builder;
    }

    public static IApplicationBuilder UseEndpoints(this IApplicationBuilder builder)
    {
        builder.Use(async (context, next) =>
        {
            if (context.FeatureCollection.TryGetValue("Endpoint", out var endpoint) && endpoint is RouteEndpoint routeEndpoint)
            {
                await routeEndpoint.Handler.Invoke(context);
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/plain";
                byte[] buffer = Encoding.UTF8.GetBytes("Not Found");
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer);
            }

            await next(context);
        });

        return builder;
    }

    public static IApplicationBuilder UseAuthorization(this IApplicationBuilder builder)
    {
        builder.Use(async (context, next) =>
        {
            if (context.FeatureCollection.TryGetValue("Endpoint", out var endpoint) && endpoint is RouteEndpoint routeEndpoint)
            {
                if (routeEndpoint.Metadata.Any(m => m is RequiresAuthorizationMetadata))
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "text/plain";
                    byte[] buffer = Encoding.UTF8.GetBytes("Unauthorized");
                    context.Response.ContentLength64 = buffer.Length;
                    await context.Response.OutputStream.WriteAsync(buffer);
                }

                await next(context);
            }
        });

        return builder;
    }
}