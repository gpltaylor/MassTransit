namespace MassTransitBenchmark.BusOutbox;

using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


public class MigrationHostedService<TDbContext> :
    IHostedService
    where TDbContext : DbContext
{
    readonly IServiceScopeFactory _scopeFactory;
    readonly ILogger<MigrationHostedService<TDbContext>> _logger;
    IServiceScope _scope;
    TDbContext _context;

    public MigrationHostedService(IServiceScopeFactory scopeFactory, ILogger<MigrationHostedService<TDbContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying migrations for {DbContext}", TypeCache<TDbContext>.ShortName);

        _scope = _scopeFactory.CreateScope();

        _context = _scope.ServiceProvider.GetRequiredService<TDbContext>();

        await _context.Database.EnsureDeletedAsync(cancellationToken);
        await _context.Database.EnsureCreatedAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}
