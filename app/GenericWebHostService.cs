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