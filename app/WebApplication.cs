using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class WebApplicationBuilder : IHostApplicationBuilder
{
    private IHostApplicationBuilder _innerBuilder = new HostApplicationBuilder();
    private WebApplication? _builtApplication = null;
    public IDictionary<object, object> Properties => _innerBuilder.Properties;

    public IConfigurationManager Configuration => _innerBuilder.Configuration;

    public IHostEnvironment Environment => _innerBuilder.Environment;

    public ILoggingBuilder Logging => _innerBuilder.Logging;

    public IMetricsBuilder Metrics => _innerBuilder.Metrics;
    public IServiceCollection Services => _innerBuilder.Services;

    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) where TContainerBuilder : notnull
    {
        _innerBuilder.ConfigureContainer(factory, configure);
    }

    public WebApplication Build()
    {
        _innerBuilder.Services.AddSingleton<IServer, Server>();
        _innerBuilder.Services.AddHostedService<GenericWebHostService>(services =>
        {
            return new GenericWebHostService(_builtApplication, services.GetRequiredService<IServer>(), services);
        });
        var host = ((HostApplicationBuilder)_innerBuilder).Build();
        _builtApplication = new WebApplication(host);
        return _builtApplication;
    }
}

public class WebApplication(IHost host) : IHost, IApplicationBuilder, IEndpointRouteBuilder
{
    public static WebApplicationBuilder CreateBuilder() => new();
    public IServiceProvider Services => host.Services;

    public List<Func<RequestDelegate, RequestDelegate>> Middlewares { get; } = [];

    private Dictionary<string, EndpointBuilder> _endpoints = new();
    Dictionary<string, RouteEndpoint> IEndpointRouteBuilder.Endpoints
    {
        get
        {
            return _endpoints.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build());
        }
    }

    public void Dispose()
        => host.Dispose();

    public Task StartAsync(CancellationToken cancellationToken = default)
        => host.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
        => host.StopAsync(cancellationToken);

    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        Middlewares.Add(middleware);
        return this;
    }

    public IApplicationBuilder Use(Func<HttpContext, RequestDelegate, Task> middleware)
    {
        return Use(next => context => middleware(context, next));
    }

    public RequestDelegate Build()
    {
        var handler = new RequestDelegate(context => Task.CompletedTask);
        for (var c = Middlewares.Count - 1; c >= 0; c--)
        {
            handler = Middlewares[c](handler);
        }
        return handler;
    }

    IEndpointConventionBuilder IEndpointRouteBuilder.AddRouteHandler(string path, string method, RequestDelegate handler)
    {
        var conventions = new List<Action<EndpointBuilder>>();
        _endpoints[path] = new EndpointBuilder
        {
            Path = path,
            Method = method,
            Handler = handler,
            Conventions = conventions
        };
        return new EndpointConventionBuilder(conventions);
    }
}