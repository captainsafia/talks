using System.Net;
using System.Text;

var port = 8080;
var server = new Server(port);

server.UseAuthentication();
server.UseRouting();
server.UseNotFoundPage();

server.Map("/hello", (string name) => new OkResult($"Hello {name}!"));
server.Map("/parsed-date", (DateTime date) => new OkResult($"The date is {date}!"));

server.Start();
Console.WriteLine($"Listening on port {port}. Press `Esc` to close server...");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) {}
server.Stop();
Console.WriteLine("Exiting the server...");

public delegate Task RequestDelegate(HttpListenerContext context);

public class Server(int port)
{
    private HttpListener? _listener;
    private readonly Dictionary<string, Func<HttpListenerContext, IResult>> _handlers = new();
    private readonly List<Func<RequestDelegate, RequestDelegate>> _middlewares = [];

    public Dictionary<string, Func<HttpListenerContext, IResult>> Handlers { get => _handlers; }

    public void Start()
    {
        // Create an instance of the HttpListener that listens on a
        // user defined port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        // Receive events (aka requests) sent to the server
        // and process them in a listener loop.
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

    // Map a handler to an associated path.
    public void Map(string path, Func<HttpListenerContext, IResult> handler) => 
        _handlers.Add(path, handler);

    // Map a handler that accepts a parameter to be bound from
    // the query.
    public void Map<T>(string path, Func<T, IResult> handler) where T: IParsable<T>
    {
        Func<HttpListenerContext, IResult> wrappedHandler = (context) =>
        {
            var name = handler.Method.GetParameters().Single().Name;
            var unparsedParam = context.Request.QueryString.Get(name);
            var result = T.Parse(unparsedParam, System.Globalization.CultureInfo.InvariantCulture);
            return handler.Invoke(result);
        };
        _handlers.Add(path, wrappedHandler);
    }

    // Register a middleware into our application pipeline.
    public void Use(Func<RequestDelegate, RequestDelegate> middleware) =>
        _middlewares.Add(middleware);

    // Register a middleware within our application pipeline
    // with support for wrapping it into the `RequestDelegate`
    // type.
    public void Use(Func<HttpListenerContext, RequestDelegate, Task> middleware) =>
        Use(next => context => middleware(context, next));

    // Invoked when our handler proceses a new request.
    private void Callback(IAsyncResult asyncResult)
    {
        // Do nothing in case the listener has already been disposed.
        if (_listener == null)
        {
            return;
        }

        HttpListenerContext ctx = _listener.EndGetContext (asyncResult);
        // Invoke the middleware pipeline.
        RequestDelegate handler = context => Task.CompletedTask;
        for (var c = _middlewares.Count - 1; c >= 0; c--)
        {
            handler = _middlewares[c](handler);
        }
        handler.Invoke(ctx);

        // Process the next request.
        _listener.BeginGetContext(new AsyncCallback(Callback), _listener);
    }
}

// An interface that represents a result type
// that should be returned from a handler.
public interface IResult
{
    void Execute(HttpListenerContext ctx);
}

// Implement a result type that returns a
// `text/plain` response with a 200 OK status
// code.
public class OkResult(string content) : IResult
{
    public void Execute(HttpListenerContext ctx)
    {
        using HttpListenerResponse resp = ctx.Response;

        // Set HTTP headers and status codes on the response.
        resp.Headers.Set("Content-Type", "text/plain");
        resp.StatusCode = 200;
        resp.StatusDescription = "OK";

        // Generate a byte string representing the response that
        // we would like to write to the request.
        byte[] buffer = Encoding.UTF8.GetBytes(content);
        resp.ContentLength64 = buffer.Length;

        // Write the response to the output stream.
        using var outputStream = resp.OutputStream;
        outputStream.Write(buffer, 0, buffer.Length);
    }
}

// Static extension methods for registering middlewares
// into our application pipeline.
public static class ServerExtensions
{
    public static Server UseRouting(this Server server)
    {
        server.Use((ctx, next) => {
            if (ctx.Request.Url?.AbsolutePath is {} targetPath && server.Handlers.TryGetValue(targetPath, out var handler))
            {
                var result = handler.Invoke(ctx);
                result.Execute(ctx);
            }
            else
            {
                ctx.Response.StatusCode = 404;
            }
            return next(ctx);
        });
        return server;
    }

    public static Server UseAuthentication(this Server server)
    {
        server.Use((ctx, next) =>
        {
            if (ctx.Request.Headers.Get("Authorization") is string value)
            {
                return next(ctx);
            }
            else
            {
                using HttpListenerResponse resp = ctx.Response;
                resp.StatusCode = 401;
                resp.StatusDescription = "Not Authorzied";
                return Task.CompletedTask;
            }
        });
        return server;
    }

    public static Server UseNotFoundPage(this Server server)
    {
        server.Use((ctx, next) =>
        {
            if (ctx.Response.StatusCode == 404)
            {
                ctx.Response.Headers.Set("Content-Type", "text/html");
                ctx.Response.StatusDescription = "Not Found";
                byte[] buffer = Encoding.UTF8.GetBytes("""
                    <html>
                        <head>
                            <title>404: Not Found</title>
                        </head>
                        <body>
                            <h1>The page you are looking for could not be found.</h1>
                        </body>
                    </html>
                """);
                ctx.Response.ContentLength64 = buffer.Length;
                using var outputStream = ctx.Response.OutputStream;
                outputStream.Write(buffer, 0, buffer.Length);
                return Task.CompletedTask;
            }
            return next(ctx);
        });
        return server;
    }
}