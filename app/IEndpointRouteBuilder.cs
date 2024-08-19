public interface IEndpointRouteBuilder 
{
    Dictionary<string, RouteEndpoint> Endpoints { get; }

    IEndpointConventionBuilder AddRouteHandler(string path, string method, RequestDelegate handler);
}

public class EndpointBuilder
{
    public required string Path { get; set; }
    public required string Method { get; set; }
    public required RequestDelegate Handler { get; set; }
    public List<object?> Metadata { get; } = [];
    public List<Action<EndpointBuilder>> Conventions { get; set; } = [];

    public RouteEndpoint Build()
    {
        foreach (var convention in Conventions)
        {
            convention(this);
        }
        return new RouteEndpoint
        {
            Route = Path,
            Method = Method,
            Handler = Handler,
            Metadata = Metadata.AsReadOnly()
        };
    }
}

public class RouteEndpoint
{
    public required string Route { get; set; }

    public required string Method { get; set; }
    
    public required RequestDelegate Handler { get; set; }

    public required IReadOnlyList<object?> Metadata { get; set; }
}


public interface IEndpointConventionBuilder
{
    void Add(Action<EndpointBuilder> action);
}

public class EndpointConventionBuilder(List<Action<EndpointBuilder>> conventions) : IEndpointConventionBuilder
{
    public void Add(Action<EndpointBuilder> action)
    {
        conventions.Add(action);
    }
}

public static class IEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder Map(this IEndpointRouteBuilder builder, string path, Delegate handler)
    {
        var requestDelegate = RequestDelegateFactory.CreateRequestDelegate(handler);
        return builder.AddRouteHandler(path, "GET", requestDelegate);
    }

    public static IEndpointConventionBuilder MapPost(this IEndpointRouteBuilder builder, string path, Delegate handler)
    {
        var requestDelegate = RequestDelegateFactory.CreateRequestDelegate(handler);
        return builder.AddRouteHandler(path, "POST", requestDelegate);
    }
}