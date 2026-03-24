using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OrderService.Api.Triggers;
using OrderService.Application.Common.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Api.Specs.Support;

public sealed class PlaceOrderLocalQueueWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestConnectionString = "Host=localhost;Port=5432;Database=orderservice-tests;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("MessageProcessing:Mode", "Function");
        builder.UseSetting("MessageQueue:UseLocal", "true");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:OrderServiceDb"] = TestConnectionString,
                ["MessageQueue:ConnectionString"] = TestConnectionString,
                ["MessageProcessing:Mode"] = "Function",
                ["MessageQueue:UseLocal"] = "true",
                ["AzureServiceBus:ConnectionString"] = "UseDevelopmentStorage=true"
            };

            configurationBuilder.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureServices(services =>
        {
            var hostedServicesToRemove = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                    && (descriptor.ImplementationType == typeof(DatabaseInitializationHostedService)
                        || descriptor.ImplementationType == typeof(SupplierQuoteQueueTriggerBackgroundService)))
                .ToList();

            foreach (var descriptor in hostedServicesToRemove)
            {
                services.Remove(descriptor);
            }

            // Keep place-order deterministic by providing a known product catalogue.
            services.RemoveAll<IProductRepository>();
            services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderServiceDbContext>();
        dbContext.Database.EnsureCreated();

        return host;
    }
}
