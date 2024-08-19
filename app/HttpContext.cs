using System.Net;

public class HttpContext
{
    public required HttpListenerRequest Request { get; set; }
    public required HttpListenerResponse Response { get; set; }
    public required IServiceProvider ServiceProvider { get; set; }
    public Dictionary<string, object> FeatureCollection { get; set; } = [];
}