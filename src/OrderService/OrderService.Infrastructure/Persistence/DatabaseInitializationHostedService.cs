using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OrderService.Infrastructure.Persistence;

public sealed class DatabaseInitializationHostedService(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderServiceDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "OrdersTopic" (
                "Id" BIGSERIAL PRIMARY KEY,
                "OrderId" UUID NOT NULL,
                "SupplierCode" VARCHAR(32) NOT NULL,
                "Payload" JSONB NOT NULL,
                "EnqueuedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                "ProcessedAtUtc" TIMESTAMPTZ NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_OrdersTopic_SupplierCode_ProcessedAtUtc_Id"
                ON "OrdersTopic" ("SupplierCode", "ProcessedAtUtc", "Id");

            CREATE TABLE IF NOT EXISTS "SupplierQuotes" (
                "Id" BIGSERIAL PRIMARY KEY,
                "OrderId" UUID NOT NULL,
                "Payload" JSONB NOT NULL,
                "EnqueuedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                "ProcessedAtUtc" TIMESTAMPTZ NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_SupplierQuotes_ProcessedAtUtc_Id"
                ON "SupplierQuotes" ("ProcessedAtUtc", "Id");
            """,
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
