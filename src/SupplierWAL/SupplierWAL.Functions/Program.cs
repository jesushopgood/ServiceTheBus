using common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SupplierWAL.Application.Common;
using SupplierWAL.Infrastructure;
using SupplierWAL.Infrastructure.Messaging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, configurationBuilder) =>
    {
        configurationBuilder
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplication();
        services.AddInfrastructure(context.Configuration);
        services
            .AddOptions<MessageProcessingOptions>()
            .Bind(context.Configuration.GetSection(MessageProcessingOptions.SectionName))
            .Validate(static options => Enum.TryParse<MessageProcessingMode>(options.Mode, ignoreCase: true, out _),
                "MessageProcessing:Mode must be one of Function|Service.")
            .ValidateOnStart();
    })
    .Build();

host.Run();
