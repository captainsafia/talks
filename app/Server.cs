using System.Net;

public interface IServer
{
    Task StartAsync(RequestDelegate handler, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public class Server : IServer
{
    private HttpListener? _listener;

    public Task StartAsync(RequestDelegate handler, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:8080/");
        _listener.Start();
        _listener.BeginGetContext(new AsyncCallback(result => Callback(result, handler, serviceProvider)), _listener);
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

    private void Callback(IAsyncResult asyncResult, RequestDelegate handler, IServiceProvider serviceProvider)
    {
        if (_listener == null)
        {
            return;
        }

        HttpListenerContext ctx = _listener.EndGetContext(asyncResult);
        HttpContext httpContext = new HttpContext
        {
            Response = ctx.Response,
            Request = ctx.Request,
            ServiceProvider = serviceProvider
        };
        handler.Invoke(httpContext);

        _listener.BeginGetContext(new AsyncCallback(result => Callback(result, handler, serviceProvider)), _listener);
    }
}