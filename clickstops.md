## Clickstop 1: Basic HTTP server with listen/loop

```csharp
using System.Net;
using System.Text;

var port = 8080;
var server = new Server(port);

server.Start();
Console.WriteLine($"Listening on http://localhost:{port}. Press `Esc` to close server...");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) {}
server.Stop();
Console.WriteLine("Exiting the server...");

public class Server(int port)
{
    private HttpListener? _listener;

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }

    public void Stop()
    {
        if (_listener != null)
        {
            _listener.Close();
            _listener = null;
        }
    }

    private void Callback(IAsyncResult asyncResult)
    {
        if (_listener == null)
        {
            return;
        }

        HttpListenerContext ctx = _listener.EndGetContext(asyncResult);
        using HttpListenerResponse response = ctx.Response;
        response.ContentType = "text/plain";
        response.StatusCode = 200;
        
        byte[] buffer = Encoding.UTF8.GetBytes("Hello world!");
        response.ContentLength64 = buffer.Length;

        using var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);

        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }
}
```

## Clickstop 2: Server with `AddHandler`

```csharp
using System.Net;
using System.Text;

var port = 8080;
var server = new Server(port);

server.AddHandler((ctx) => 
{
    using HttpListenerResponse response = ctx.Response;
    response.ContentType = "text/plain";
    response.StatusCode = 200;
    
    byte[] buffer = Encoding.UTF8.GetBytes("Hello world again!");
    response.ContentLength64 = buffer.Length;

    using var output = response.OutputStream;
    output.Write(buffer, 0, buffer.Length);
});

server.Start();
Console.WriteLine($"Listening on http://localhost:{port}. Press `Esc` to close server...");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) {}
server.Stop();
Console.WriteLine("Exiting the server...");

public class Server(int port)
{
    private HttpListener? _listener;
    private Action<HttpListenerContext>? _handler;

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }

    public void AddHandler(Action<HttpListenerContext> handler)
    {
        _handler = handler;
    }

    public void Stop()
    {
        if (_listener != null)
        {
            _listener.Close();
            _listener = null;
        }
    }

    private void Callback(IAsyncResult asyncResult)
    {
        if (_listener == null)
        {
            return;
        }

        HttpListenerContext ctx = _listener.EndGetContext(asyncResult);
        if (_handler != null)
        {
            _handler(ctx);
        }
        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }
}
```

## Clickstop 3: Multiple handlers, `RouteEndpoint`, and basic routing

```csharp
using System.Net;
using System.Text;

var port = 8080;
var server = new Server(port);

server.Map("/", (ctx) => 
{
    using HttpListenerResponse response = ctx.Response;
    response.ContentType = "text/plain";
    response.StatusCode = 200;
    
    byte[] buffer = Encoding.UTF8.GetBytes("Hello world 2!");
    response.ContentLength64 = buffer.Length;

    using var output = response.OutputStream;
    output.Write(buffer, 0, buffer.Length);
});

server.Start();
Console.WriteLine($"Listening on http://localhost:{port}. Press `Esc` to close server...");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) {}
server.Stop();
Console.WriteLine("Exiting the server...");

public class Server(int port)
{
    private HttpListener? _listener;
    private Dictionary<string, RouteEndpoint> _endpoints = [];

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }

    public void Map(string route, Action<HttpListenerContext> handler)
    {
        _endpoints.Add(route, new RouteEndpoint { Route = route, Handler = handler });
    }

    public void Stop()
    {
        if (_listener != null)
        {
            _listener.Close();
            _listener = null;
        }
    }

    private void Callback(IAsyncResult asyncResult)
    {
        if (_listener == null)
        {
            return;
        }

        HttpListenerContext ctx = _listener.EndGetContext(asyncResult);
        
        if (ctx.Request.Url?.AbsolutePath is {} targetPath && _endpoints.TryGetValue(targetPath, out var endpoint))
        {
            endpoint.Handler(ctx);
        }
        else
        {
            using HttpListenerResponse response = ctx.Response;
            response.ContentType = "text/plain";
            response.StatusCode = 400;
            
            byte[] buffer = Encoding.UTF8.GetBytes("Not found!");
            response.ContentLength64 = buffer.Length;

            using var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
        }

        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }
}

public class RouteEndpoint
{
    public required string Route { get; set; }
    public required Action<HttpListenerContext> Handler { get; set; }
}
```

## Clickstop 4: Middleware infrastructure

```csharp
using System.Net;
using System.Text;

var port = 8080;
var server = new Server(port);

server.Map("/", (ctx) => 
{
    using HttpListenerResponse response = ctx.Response;
    response.ContentType = "text/plain";
    response.StatusCode = 200;
    
    byte[] buffer = Encoding.UTF8.GetBytes("Hello world 2!");
    response.ContentLength64 = buffer.Length;

    using var output = response.OutputStream;
    output.Write(buffer, 0, buffer.Length);
});

server.Start();
Console.WriteLine($"Listening on http://localhost:{port}. Press `Esc` to close server...");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) {}
server.Stop();
Console.WriteLine("Exiting the server...");

public class Server(int port)
{
    private HttpListener? _listener;
    private Dictionary<string, RouteEndpoint> _endpoints = [];
    private List<Func<RequestDelegate, RequestDelegate>> _middlewares = [];

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }

    public void Map(string route, Action<HttpListenerContext> handler)
    {
        _endpoints.Add(route, new RouteEndpoint { Route = route, Handler = handler });
    }

    public void Stop()
    {
        if (_listener != null)
        {
            _listener.Close();
            _listener = null;
        }
    }

    public void Use(Func<RequestDelegate, RequestDelegate> middleware) =>
        _middlewares.Add(middleware);

    public void Use(Func<HttpContext, RequestDelegate, Task> middleware) =>
        Use(next => context => middleware(context, next));

    private void Callback(IAsyncResult asyncResult)
    {
        if (_listener == null)
        {
            return;
        }

        HttpListenerContext ctx = _listener.EndGetContext(asyncResult);
        
        if (ctx.Request.Url?.AbsolutePath is {} targetPath && _endpoints.TryGetValue(targetPath, out var endpoint))
        {
            endpoint.Handler(ctx);
        }
        else
        {
            using HttpListenerResponse response = ctx.Response;
            response.ContentType = "text/plain";
            response.StatusCode = 400;
            
            byte[] buffer = Encoding.UTF8.GetBytes("Not found!");
            response.ContentLength64 = buffer.Length;

            using var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
        }

        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }
}

public class RouteEndpoint
{
    public required string Route { get; set; }
    public required Action<HttpListenerContext> Handler { get; set; }
}

public class HttpContext
{
    public required HttpListenerRequest Request { get; set; }
    public required HttpListenerResponse Response { get; set; }
    public Dictionary<string, object> FeatureCollection { get; set; } = [];
}

public delegate Task RequestDelegate(HttpContext httpContext);
```

## Clickstop 5: `UseEndpoints`, `UseRouting`, and invoke middlewares

```csharp
using System.Net;
using System.Text;

var port = 8080;
var server = new Server(port);

server.UseRouting();
server.UseEndpoints();

server.Map("/", async (ctx) => 
{
    using HttpListenerResponse response = ctx.Response;
    response.ContentType = "text/plain";
    response.StatusCode = 200;
    
    byte[] buffer = Encoding.UTF8.GetBytes("Hello world 2!");
    response.ContentLength64 = buffer.Length;

    using var output = response.OutputStream;
    await output.WriteAsync(buffer);
});

server.Start();
Console.WriteLine($"Listening on http://localhost:{port}. Press `Esc` to close server...");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) {}
server.Stop();
Console.WriteLine("Exiting the server...");

public class Server(int port)
{
    private HttpListener? _listener;
    private Dictionary<string, RouteEndpoint> _endpoints = [];
    private List<Func<RequestDelegate, RequestDelegate>> _middlewares = [];

    public Dictionary<string, RouteEndpoint> Endpoints => _endpoints;

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }

    public void Map(string route, RequestDelegate handler)
    {
        _endpoints.Add(route, new RouteEndpoint { Route = route, Handler = handler });
    }

    public void Stop()
    {
        if (_listener != null)
        {
            _listener.Close();
            _listener = null;
        }
    }

    public void Use(Func<RequestDelegate, RequestDelegate> middleware) =>
        _middlewares.Add(middleware);

    public void Use(Func<HttpContext, RequestDelegate, Task> middleware) =>
        Use(next => context => middleware(context, next));

    private void Callback(IAsyncResult asyncResult)
    {
        if (_listener == null)
        {
            return;
        }

        HttpListenerContext ctx = _listener.EndGetContext(asyncResult);
        var httpContext = new HttpContext
        {
            Request = ctx.Request,
            Response = ctx.Response
        };
        RequestDelegate handler = context => Task.CompletedTask;
        for (var c = _middlewares.Count - 1; c >= 0; c--)
        {
            handler = _middlewares[c](handler);
        }
        handler.Invoke(httpContext);

        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }
}

public class RouteEndpoint
{
    public required string Route { get; set; }
    public required RequestDelegate Handler { get; set; }
}

public class HttpContext
{
    public required HttpListenerRequest Request { get; set; }
    public required HttpListenerResponse Response { get; set; }
    public Dictionary<string, object> FeatureCollection { get; set; } = [];
}

public delegate Task RequestDelegate(HttpContext httpContext);

public static class ServerExtensions
{
    public static Server UseRouting(this Server server)
    {
        server.Use((context, next) =>
        {
            if (context.Request.Url?.AbsolutePath is {} targetPath && server.Endpoints.TryGetValue(targetPath, out var endpoint))
            {
                Console.WriteLine("here 2");
                context.FeatureCollection.Add("Endpoint", endpoint);
            }

            return next(context);
        });

        return server;
    }

    public static Server UseEndpoints(this Server server)
    {
        server.Use(async (context, next) =>
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

        return server;
    }
}
```

## Clickstop 6: `RequestDelegateFactory`, `ParameterBindingMethodCache`

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text;

var port = 8080;
var server = new Server(port);

server.UseRouting();
server.UseEndpoints();

server.Map("/", async (HttpContext ctx, string name, int age) => 
{
    using HttpListenerResponse response = ctx.Response;
    response.ContentType = "text/plain";
    response.StatusCode = 200;
    
    byte[] buffer = Encoding.UTF8.GetBytes($"You're name is {name} and you're {age} years old.");;
    response.ContentLength64 = buffer.Length;

    using var output = response.OutputStream;
    await output.WriteAsync(buffer);
});

server.Start();
Console.WriteLine($"Listening on http://localhost:{port}. Press `Esc` to close server...");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) {}
server.Stop();
Console.WriteLine("Exiting the server...");

public class Server(int port)
{
    private HttpListener? _listener;
    private Dictionary<string, RouteEndpoint> _endpoints = [];
    private List<Func<RequestDelegate, RequestDelegate>> _middlewares = [];

    public Dictionary<string, RouteEndpoint> Endpoints => _endpoints;

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }

    public void Map(string route, Delegate handler)
    {
        var requestDelegate = RequestDelegateFactory.CreateRequestDelegate(handler);
        _endpoints.Add(route, new RouteEndpoint { Route = route, Handler = requestDelegate });
    }

    public void Stop()
    {
        if (_listener != null)
        {
            _listener.Close();
            _listener = null;
        }
    }

    public void Use(Func<RequestDelegate, RequestDelegate> middleware) =>
        _middlewares.Add(middleware);

    public void Use(Func<HttpContext, RequestDelegate, Task> middleware) =>
        Use(next => context => middleware(context, next));

    private void Callback(IAsyncResult asyncResult)
    {
        if (_listener == null)
        {
            return;
        }

        HttpListenerContext ctx = _listener.EndGetContext(asyncResult);
        var httpContext = new HttpContext
        {
            Request = ctx.Request,
            Response = ctx.Response
        };
        RequestDelegate handler = context => Task.CompletedTask;
        for (var c = _middlewares.Count - 1; c >= 0; c--)
        {
            handler = _middlewares[c](handler);
        }
        handler.Invoke(httpContext);

        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }
}

public class RouteEndpoint
{
    public required string Route { get; set; }
    public required RequestDelegate Handler { get; set; }
}

public class HttpContext
{
    public required HttpListenerRequest Request { get; set; }
    public required HttpListenerResponse Response { get; set; }
    public Dictionary<string, object> FeatureCollection { get; set; } = [];
}

public delegate Task RequestDelegate(HttpContext httpContext);

public static class ServerExtensions
{
    public static Server UseRouting(this Server server)
    {
        server.Use((context, next) =>
        {
            if (context.Request.Url?.AbsolutePath is {} targetPath && server.Endpoints.TryGetValue(targetPath, out var endpoint))
            {
                context.FeatureCollection.Add("Endpoint", endpoint);
            }

            return next(context);
        });

        return server;
    }

    public static Server UseEndpoints(this Server server)
    {
        server.Use(async (context, next) =>
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

        return server;
    }
}

public static class RequestDelegateFactory
{
    public static RequestDelegate CreateRequestDelegate(Delegate routeHandler)
    {
        var parameters = routeHandler.Method.GetParameters();
        RequestDelegate delegateHandler = context =>
        {
            var arguments = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                if (ParameterBindingMethodCache.TryGetTryParseMethod(parameters[i].ParameterType, out var tryParseMethod))
                {
                    var queryValue = context.Request.QueryString[parameters[i].Name];
                    if (queryValue == null)
                    {
                        arguments[i] = null;
                    }
                    object?[] invokedArgs = [queryValue, null];
                    tryParseMethod.Invoke(null, invokedArgs);
                    arguments[i] = invokedArgs[1];
                }
                else if (parameters[i].ParameterType == typeof(HttpContext))
                {
                    arguments[i] = context;
                }
                else if (parameters[i].ParameterType == typeof(string))
                {
                    arguments[i] = context.Request.QueryString[parameters[i].Name];
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported parameter type: {parameters[i].ParameterType}");
                }
            }
            routeHandler.DynamicInvoke(arguments); 
            return Task.CompletedTask; 
        };
        return delegateHandler;
    }
}

public static class ParameterBindingMethodCache
{
    private static readonly Dictionary<Type, MethodInfo> _tryParseCache = new();

    public static bool TryGetTryParseMethod(this Type type, [NotNullWhen(true)] out MethodInfo? methodInfo)
    {
        methodInfo = null;
        if (_tryParseCache.TryGetValue(type, out var cachedMethodInfo))
        {
            methodInfo = cachedMethodInfo;
            return true;
        }

        var tryParseMethod = type.GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static, null,
            [typeof(string), type.MakeByRefType()],
            null);
        if (tryParseMethod == null)
        {
            return false;
        }
        else
        {
            _tryParseCache[type] = tryParseMethod;
            methodInfo = tryParseMethod;
            return true;
        }
    }
}
```



## Clickstop 7: Support for `IResult` type

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text;

var port = 8080;
var server = new Server(port);

server.UseRouting();
server.UseEndpoints();

server.Map("/", (string name, int age) => 
{
    return new OkResult($"You're name is {name} and you're {age} years old.");
});

server.Start();
Console.WriteLine($"Listening on http://localhost:{port}. Press `Esc` to close server...");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) {}
server.Stop();
Console.WriteLine("Exiting the server...");

public class Server(int port)
{
    private HttpListener? _listener;
    private Dictionary<string, RouteEndpoint> _endpoints = [];
    private List<Func<RequestDelegate, RequestDelegate>> _middlewares = [];

    public Dictionary<string, RouteEndpoint> Endpoints => _endpoints;

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }

    public void Map(string route, Delegate handler)
    {
        var requestDelegate = RequestDelegateFactory.CreateRequestDelegate(handler);
        _endpoints.Add(route, new RouteEndpoint { Route = route, Handler = requestDelegate });
    }

    public void Stop()
    {
        if (_listener != null)
        {
            _listener.Close();
            _listener = null;
        }
    }

    public void Use(Func<RequestDelegate, RequestDelegate> middleware) =>
        _middlewares.Add(middleware);

    public void Use(Func<HttpContext, RequestDelegate, Task> middleware) =>
        Use(next => context => middleware(context, next));

    private void Callback(IAsyncResult asyncResult)
    {
        if (_listener == null)
        {
            return;
        }

        HttpListenerContext ctx = _listener.EndGetContext(asyncResult);
        var httpContext = new HttpContext
        {
            Request = ctx.Request,
            Response = ctx.Response
        };
        RequestDelegate handler = context => Task.CompletedTask;
        for (var c = _middlewares.Count - 1; c >= 0; c--)
        {
            handler = _middlewares[c](handler);
        }
        handler.Invoke(httpContext);

        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }
}

public class RouteEndpoint
{
    public required string Route { get; set; }
    public required RequestDelegate Handler { get; set; }
}

public class HttpContext
{
    public required HttpListenerRequest Request { get; set; }
    public required HttpListenerResponse Response { get; set; }
    public Dictionary<string, object> FeatureCollection { get; set; } = [];
}

public delegate Task RequestDelegate(HttpContext httpContext);

public static class ServerExtensions
{
    public static Server UseRouting(this Server server)
    {
        server.Use((context, next) =>
        {
            if (context.Request.Url?.AbsolutePath is {} targetPath && server.Endpoints.TryGetValue(targetPath, out var endpoint))
            {
                context.FeatureCollection.Add("Endpoint", endpoint);
            }

            return next(context);
        });

        return server;
    }

    public static Server UseEndpoints(this Server server)
    {
        server.Use(async (context, next) =>
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

        return server;
    }
}

public static class RequestDelegateFactory
{
    public static RequestDelegate CreateRequestDelegate(Delegate routeHandler)
    {
        var parameters = routeHandler.Method.GetParameters();
        RequestDelegate delegateHandler = context =>
        {
            var arguments = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                if (ParameterBindingMethodCache.TryGetTryParseMethod(parameters[i].ParameterType, out var tryParseMethod))
                {
                    var queryValue = context.Request.QueryString[parameters[i].Name];
                    if (queryValue == null)
                    {
                        arguments[i] = null;
                    }
                    object?[] invokedArgs = [queryValue, null];
                    tryParseMethod.Invoke(null, invokedArgs);
                    arguments[i] = invokedArgs[1];
                }
                else if (parameters[i].ParameterType == typeof(HttpContext))
                {
                    arguments[i] = context;
                }
                else if (parameters[i].ParameterType == typeof(string))
                {
                    arguments[i] = context.Request.QueryString[parameters[i].Name];
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported parameter type: {parameters[i].ParameterType}");
                }
            }
            var result = routeHandler.DynamicInvoke(arguments); 
            if (result is IResult resultHandler)
            {
                resultHandler.Execute(context.Response);
            }
            return Task.CompletedTask; 
        };
        return delegateHandler;
    }
}

public static class ParameterBindingMethodCache
{
    private static readonly Dictionary<Type, MethodInfo> _tryParseCache = new();

    public static bool TryGetTryParseMethod(this Type type, [NotNullWhen(true)] out MethodInfo? methodInfo)
    {
        methodInfo = null;
        if (_tryParseCache.TryGetValue(type, out var cachedMethodInfo))
        {
            methodInfo = cachedMethodInfo;
            return true;
        }

        var tryParseMethod = type.GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static, null,
            [typeof(string), type.MakeByRefType()],
            null);
        if (tryParseMethod == null)
        {
            return false;
        }
        else
        {
            _tryParseCache[type] = tryParseMethod;
            methodInfo = tryParseMethod;
            return true;
        }
    }
}

public interface IResult
{
    void Execute(HttpListenerResponse response);
}

public class OkResult(string content) : IResult
{
    public void Execute(HttpListenerResponse response)
    {
        response.ContentType = "text/plain";
        response.StatusCode = 200;
        byte[] buffer = Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;
        using var output = response.OutputStream;
        output.Write(buffer);
    }
}
```

**move framework code to separate file**

## Clickstop 8: Support for routing via HTTP methods

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text;

public class Server(int port)
{
    private HttpListener? _listener;
    private Dictionary<string, RouteEndpoint> _endpoints = [];
    private List<Func<RequestDelegate, RequestDelegate>> _middlewares = [];

    public Dictionary<string, RouteEndpoint> Endpoints => _endpoints;

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }

    public void Map(string route, Delegate handler)
    {
        var requestDelegate = RequestDelegateFactory.CreateRequestDelegate(handler);
        _endpoints.Add(route, new RouteEndpoint { Route = route, Handler = requestDelegate });
    }

    public void MapPost(string route, Delegate handler)
    {

        var requestDelegate = RequestDelegateFactory.CreateRequestDelegate(handler);
        _endpoints.Add(route, new RouteEndpoint { Route = route, Method = "POST", Handler = requestDelegate });
    }

    public void Stop()
    {
        if (_listener != null)
        {
            _listener.Close();
            _listener = null;
        }
    }

    public void Use(Func<RequestDelegate, RequestDelegate> middleware) =>
        _middlewares.Add(middleware);

    public void Use(Func<HttpContext, RequestDelegate, Task> middleware) =>
        Use(next => context => middleware(context, next));

    private void Callback(IAsyncResult asyncResult)
    {
        if (_listener == null)
        {
            return;
        }

        HttpListenerContext ctx = _listener.EndGetContext(asyncResult);
        var httpContext = new HttpContext
        {
            Request = ctx.Request,
            Response = ctx.Response
        };
        RequestDelegate handler = context => Task.CompletedTask;
        for (var c = _middlewares.Count - 1; c >= 0; c--)
        {
            handler = _middlewares[c](handler);
        }
        handler.Invoke(httpContext);

        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }
}

public class RouteEndpoint
{
    public required string Route { get; set; }

    public string Method { get; set; } = "GET";
    
    public required RequestDelegate Handler { get; set; }
}

public class HttpContext
{
    public required HttpListenerRequest Request { get; set; }
    public required HttpListenerResponse Response { get; set; }
    public Dictionary<string, object> FeatureCollection { get; set; } = [];
}

public delegate Task RequestDelegate(HttpContext httpContext);

public static class ServerExtensions
{
    public static Server UseRouting(this Server server)
    {
        server.Use((context, next) =>
        {
            if (context.Request.Url?.AbsolutePath is {} targetPath
                && server.Endpoints.TryGetValue(targetPath, out var endpoint)
                && endpoint.Method.Equals(context.Request.HttpMethod, StringComparison.OrdinalIgnoreCase))
            {
                context.FeatureCollection.Add("Endpoint", endpoint);
            }

            return next(context);
        });

        return server;
    }

    public static Server UseEndpoints(this Server server)
    {
        server.Use(async (context, next) =>
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

        return server;
    }
}

public static class RequestDelegateFactory
{
    public static RequestDelegate CreateRequestDelegate(Delegate routeHandler)
    {
        var parameters = routeHandler.Method.GetParameters();
        RequestDelegate delegateHandler = context =>
        {
            var arguments = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                if (ParameterBindingMethodCache.TryGetTryParseMethod(parameters[i].ParameterType, out var tryParseMethod))
                {
                    var queryValue = context.Request.QueryString[parameters[i].Name];
                    if (queryValue == null)
                    {
                        arguments[i] = null;
                    }
                    object?[] invokedArgs = [queryValue, null];
                    tryParseMethod.Invoke(null, invokedArgs);
                    arguments[i] = invokedArgs[1];
                }
                else if (parameters[i].ParameterType == typeof(HttpContext))
                {
                    arguments[i] = context;
                }
                else if (parameters[i].ParameterType == typeof(string))
                {
                    arguments[i] = context.Request.QueryString[parameters[i].Name];
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported parameter type: {parameters[i].ParameterType}");
                }
            }
            var result = routeHandler.DynamicInvoke(arguments); 
            if (result is IResult resultHandler)
            {
                resultHandler.Execute(context.Response);
            }
            return Task.CompletedTask; 
        };
        return delegateHandler;
    }
}

public static class ParameterBindingMethodCache
{
    private static readonly Dictionary<Type, MethodInfo> _tryParseCache = new();

    public static bool TryGetTryParseMethod(this Type type, [NotNullWhen(true)] out MethodInfo? methodInfo)
    {
        methodInfo = null;
        if (_tryParseCache.TryGetValue(type, out var cachedMethodInfo))
        {
            methodInfo = cachedMethodInfo;
            return true;
        }

        var tryParseMethod = type.GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static, null,
            [typeof(string), type.MakeByRefType()],
            null);
        if (tryParseMethod == null)
        {
            return false;
        }
        else
        {
            _tryParseCache[type] = tryParseMethod;
            methodInfo = tryParseMethod;
            return true;
        }
    }
}

public interface IResult
{
    void Execute(HttpListenerResponse response);
}

public class OkResult(string content) : IResult
{
    public void Execute(HttpListenerResponse response)
    {
        response.ContentType = "text/plain";
        response.StatusCode = 200;
        byte[] buffer = Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;
        using var output = response.OutputStream;
        output.Write(buffer);
    }
}
```

## Clickstop 9: Support `WebApplication` and `WebApplicationBuilder`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class WebApplicationBuilder : IHostApplicationBuilder
{
    private IHostApplicationBuilder _innerBuilder = new HostApplicationBuilder();
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
        var host = ((HostApplicationBuilder)_innerBuilder).Build();
        return new WebApplication(host);
    }
}

public class WebApplication(IHost host) : IHost
{
    public static WebApplicationBuilder CreateBuilder() => new WebApplicationBuilder();
    public IServiceProvider Services => host.Services;

    public void Dispose()
        => host.Dispose();

    public Task StartAsync(CancellationToken cancellationToken = default)
        => host.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
        => host.StopAsync(cancellationToken);
}
```

## Clickstop 10: Update `Program.cs` to use `WebApplication` and server as hosted service

```csharp
using Microsoft.Extensions.Hosting;

public class GenericWebHostService(IServer server) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
        => server.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
        => server.StopAsync(cancellationToken);
}
```

```csharp
using System.Net;

public interface IServer
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public class Server : IServer
{
    private HttpListener? _listener;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:8080/");
        _listener.Start();
        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_listener != null)
        {
            _listener.Close();
            _listener = null;
        }
        return Task.CompletedTask;
    }

    private void Callback(IAsyncResult asyncResult, RequestDelegate handler)
    {
        if (_listener == null)
        {
            return;
        }

        HttpListenerContext ctx = _listener.EndGetContext(asyncResult);
        HttpContext httpContext = new HttpContext
        {
            Response = ctx.Response,
            Request = ctx.Request
        };
        handler.Invoke(httpContext);

        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }
}
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder();

builder.Services.AddSingleton<IServer, Server>();
builder.Services.AddHostedService<GenericWebHostService>();

var app = builder.Build();

app.Run();
```

## Clickstop 11: Add `IApplicationBuilder` and supporting interfaces

```csharp
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
            // if (context.Request.Url?.AbsolutePath is {} targetPath
            //     && builder is IEndpointRouteBuilder app
            //     && app.Endpoints.TryGetValue(targetPath, out var endpoint)
            //     && endpoint.Method.Equals(context.Request.HttpMethod, StringComparison.OrdinalIgnoreCase))
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
}
```

- Add `IApplicationBuilder` to `WebApplication`

## Clickstop 12: Add `IEndpointRouteBuilder` and update routing

```csharp
public interface IEndpointRouteBuilder 
{
    Dictionary<string, RouteEndpoint> Endpoints { get; }
}

public static class IEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder Map(this IEndpointRouteBuilder builder, string path, Delegate handler)
    {
        var requestDelegate = RequestDelegateFactory.CreateRequestDelegate(handler);
        builder.Endpoints.Add(path, new RouteEndpoint { Route = path, Handler = requestDelegate });
        return builder;
    }

    public static IEndpointRouteBuilder MapPost(this IEndpointRouteBuilder builder, string path, Delegate handler)
    {
        var requestDelegate = RequestDelegateFactory.CreateRequestDelegate(handler);
        builder.Endpoints.Add(path, new RouteEndpoint { Route = path, Method = "POST", Handler = requestDelegate });
        return builder;
    }
}
```

- Add `IEndpointRouteBuilder` with explicit implementation to routing

```csharp
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

app.UseRouting();
app.UseEndpoints();

app.Map("/", (string name, int age) => 
{
    return new OkResult($"You're name is {name} and you're {age} years old.");
});
app.MapPost("/post", (int id) => 
{
    return new OkResult($"Processing on id: {id}...");
});

app.Run();
```

## Clickstop 13: Add support for resolving parameters from the DI container

```csharp
using Microsoft.Extensions.Hosting;

public class GenericWebHostService(IApplicationBuilder builder, IServer server, IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var handler = builder.Build();
        return server.StartAsync(handler, serviceProvider, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => server.StopAsync(cancellationToken);
}
```

```csharp
_innerBuilder.Services.AddHostedService<GenericWebHostService>(services =>
{
    return new GenericWebHostService(_builtApplication, services.GetRequiredService<IServer>(), services);
});
```

```csharp
var serviceProviderIsService = context.ServiceProvider.GetService<IServiceProviderIsService>();
..
else if (serviceProviderIsService is not null && serviceProviderIsService.IsService(parameters[i].ParameterType))
{
    arguments[i] = context.ServiceProvider.GetService(parameters[i].ParameterType);
}
```

```csharp
using System.Net;

public class HttpContext
{
    public required HttpListenerRequest Request { get; set; }
    public required HttpListenerResponse Response { get; set; }
    public required IServiceProvider ServiceProvider { get; set; }
    public Dictionary<string, object> FeatureCollection { get; set; } = [];
}
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder();

builder.Services.AddSingleton<Writer>();

var app = builder.Build();

app.UseRouting();
app.UseEndpoints();

app.Map("/", (string name, int age) => 
{
    return new OkResult($"You're name is {name} and you're {age} years old.");
});
app.MapPost("/post", (int id) => 
{
    return new OkResult($"Processing on id: {id}...");
});
app.Map("/writer", (string location, Writer writer) => 
{
    writer.Write($"You're location is {location}");
    return new OkResult("Check the console!");
});
app.Run();

public class Writer
{
    public void Write(string message)
    {
        Console.WriteLine(message);
    }
}
```

## Clickstop 14: Add support for resolving parameters from the JSON body

```csharp
else
{
    var parameterType = parameters[i].ParameterType;
    var parsedElement = JsonSerializer.Deserialize(context.Request.InputStream, parameterType, JsonSerializerOptions.Web);
    arguments[i] = parsedElement;
}
```

```csharp
app.MapPost("/post", (int id, Todo todo) => 
{
    return new OkResult($"Processing on id: {id} with {todo.Title}...");
});
```

## Clickstop 15: Add support for endpoint metadata and `IEndpointConventionBuilder`

```csharp
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
```

```csharp
// In WebApplication
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
```

```csharp
private Dictionary<string, EndpointBuilder> _endpoints = new();
Dictionary<string, RouteEndpoint> IEndpointRouteBuilder.Endpoints
{
    get
    {
        return _endpoints.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build());
    }
}
```

## Clickstop 16: Use metadata in authorization middleware

```csharp
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
```