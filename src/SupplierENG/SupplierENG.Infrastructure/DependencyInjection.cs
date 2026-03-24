using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using common.Messaging;
using SupplierENG.Application.Common.Interfaces;
using SupplierENG.Infrastructure.Messaging;
using SupplierENG.Infrastructure.Repositories;

namespace SupplierENG.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SupplierServiceBusOptions>(
            configuration.GetSection(SupplierServiceBusOptions.SectionName));

        if (ShouldUseLocalQueue(configuration))
        {
            services.AddScoped<IQueueService, LocalQueueService>();
        }
        else
        {
            services.AddScoped<IQueueService, LiveQueueService>();
        }

        services.AddScoped<IProductRepository, CsvProductRepository>();
        services.AddScoped<IQuotePublisher, QueueBackedQuotePublisher>();

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
