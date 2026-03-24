using common.Configuration;
using OrderService.Api.Middleware;
using OrderService.Api.Triggers;
using OrderService.Application.Common;
using OrderService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddOptions<MessageProcessingOptions>()
    .Bind(builder.Configuration.GetSection(MessageProcessingOptions.SectionName))
    .Validate(static options => Enum.TryParse<MessageProcessingMode>(options.Mode, ignoreCase: true, out _),
        "MessageProcessing:Mode must be one of Function|Service.")
    .ValidateOnStart();

var messageProcessingMode = builder.Configuration
    .GetSection(MessageProcessingOptions.SectionName)
    .Get<MessageProcessingOptions>()?
    .ResolveMode() ?? MessageProcessingMode.Function;

if (messageProcessingMode == MessageProcessingMode.Service)
{
    builder.Services.AddHostedService<SupplierQuoteQueueTriggerBackgroundService>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction() || app.Environment.IsEnvironment("Product"))
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("Message processing mode set to {MessageProcessingMode}", messageProcessingMode);

app.Run();

public partial class Program
{
}
