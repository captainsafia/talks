using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

public class HttpContext2
{
    public required HttpListenerRequest Request { get; set; }
    public required HttpListenerResponse Response { get; set; }
    public Dictionary<string, object> FeatureCollection { get; set; } = [];
}

public delegate Task RequestDelegate(HttpContext httpContext);

public static class RequestDelegateFactory
{
    public static RequestDelegate CreateRequestDelegate(Delegate routeHandler)
    {
        var parameters = routeHandler.Method.GetParameters();
        RequestDelegate delegateHandler = context =>
        {
            var arguments = new object?[parameters.Length];
            var serviceProviderIsService = context.ServiceProvider.GetService<IServiceProviderIsService>();
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
                else if (serviceProviderIsService is not null && serviceProviderIsService.IsService(parameters[i].ParameterType))
                {
                    arguments[i] = context.ServiceProvider.GetService(parameters[i].ParameterType);
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
                    var parameterType = parameters[i].ParameterType;
                    var parsedElement = JsonSerializer.Deserialize(context.Request.InputStream, parameterType, JsonSerializerOptions.Web);
                    arguments[i] = parsedElement;
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