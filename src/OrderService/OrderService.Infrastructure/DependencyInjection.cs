using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using common.Messaging;
using OrderService.Application.Common.Interfaces;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureServiceBusOptions>(
            configuration.GetSection(AzureServiceBusOptions.SectionName));

        var connectionString = configuration.GetConnectionString("OrderServiceDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:OrderServiceDb is required.");
        }

        services.AddDbContext<OrderServiceDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHostedService<DatabaseInitializationHostedService>();

        if (ShouldUseLocalQueue(configuration))
        {
            services.AddScoped<IQueueService, LocalQueueService>();
        }
        else
        {
            services.AddScoped<IQueueService, LiveQueueService>();
        }

        services.AddScoped<IOrderQueuePublisher, QueueBackedOrderQueuePublisher>();
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IQuoteAggregationRepository, EfQuoteAggregationRepository>();

        return services;
    }

    private static bool ShouldUseLocalQueue(IConfiguration configuration)
    {
        if (bool.TryParse(configuration["MessageQueue:UseLocal"], out var configured))
        {
            return configured;
        }

        return string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"],
            "Development",
            StringComparison.OrdinalIgnoreCase);
    }
}
