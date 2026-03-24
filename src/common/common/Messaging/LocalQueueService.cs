using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace common.Messaging;

public sealed class LocalQueueService(IConfiguration configuration, ILogger<LocalQueueService> logger) : IQueueService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim EnsureTablesSemaphore = new(1, 1);
    private static volatile bool _tablesEnsured;

    public async Task PublishOrderAsync(OrderPlacedMessage order, CancellationToken cancellationToken)
    {
        await EnsureTablesAsync(cancellationToken);

        var supplierCodes = ResolveSupplierCodes();
        var payload = JsonSerializer.Serialize(order, SerializerOptions);

        await using var connection = new NpgsqlConnection(ResolveConnectionString());
        await connection.OpenAsync(cancellationToken);

        foreach (var supplierCode in supplierCodes)
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO "OrdersTopic" ("OrderId", "SupplierCode", "Payload", "EnqueuedAtUtc")
                VALUES (@orderId, @supplierCode, @payload::jsonb, NOW())
                """,
                connection);

            command.Parameters.AddWithValue("orderId", order.OrderId);
            command.Parameters.AddWithValue("supplierCode", supplierCode);
            command.Parameters.AddWithValue("payload", payload);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation("Published order {OrderId} to local OrdersTopic for {SupplierCount} suppliers", order.OrderId, supplierCodes.Count);
    }

    public async Task PublishSupplierQuoteAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken)
    {
        await EnsureTablesAsync(cancellationToken);

        var payload = JsonSerializer.Serialize(quote, SerializerOptions);

        await using var connection = new NpgsqlConnection(ResolveConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO "SupplierQuotes" ("OrderId", "Payload", "EnqueuedAtUtc")
            VALUES (@orderId, @payload::jsonb, NOW())
            """,
            connection);

        command.Parameters.AddWithValue("orderId", quote.OrderId);
        command.Parameters.AddWithValue("payload", payload);

        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Published supplier quote for order {OrderId} from supplier {SupplierCode} to local SupplierQuotes", quote.OrderId, quote.SupplierCode);
    }

    public async Task<bool> ProcessNextOrderAsync(
        string supplierCode,
        Func<OrderPlacedMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        await EnsureTablesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(supplierCode))
        {
            throw new InvalidOperationException("Supplier code is required when consuming local OrdersTopic rows.");
        }

        await using var connection = new NpgsqlConnection(ResolveConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        long? id = null;
        string? payload = null;

        await using (var command = new NpgsqlCommand(
            """
            SELECT "Id", "Payload"::text
            FROM "OrdersTopic"
            WHERE "SupplierCode" = @supplierCode AND "ProcessedAtUtc" IS NULL
            ORDER BY "Id"
            FOR UPDATE SKIP LOCKED
            LIMIT 1
            """,
            connection,
            transaction))
        {
            command.Parameters.AddWithValue("supplierCode", supplierCode);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            id = reader.GetInt64(0);
            payload = reader.GetString(1);
        }

        var order = JsonSerializer.Deserialize<OrderPlacedMessage>(payload, SerializerOptions);
        if (order is null)
        {
            await MarkOrderProcessedAsync(id.Value, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogWarning("Skipped malformed local OrdersTopic row {QueueRowId}", id.Value);
            return false;
        }

        try
        {
            await handler(order, cancellationToken);
            await MarkOrderProcessedAsync(id.Value, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> ProcessNextSupplierQuoteAsync(
        Func<SupplierQuoteMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        await EnsureTablesAsync(cancellationToken);

        await using var connection = new NpgsqlConnection(ResolveConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        long? id = null;
        string? payload = null;

        await using (var command = new NpgsqlCommand(
            """
            SELECT "Id", "Payload"::text
            FROM "SupplierQuotes"
            WHERE "ProcessedAtUtc" IS NULL
            ORDER BY "Id"
            FOR UPDATE SKIP LOCKED
            LIMIT 1
            """,
            connection,
            transaction))
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            id = reader.GetInt64(0);
            payload = reader.GetString(1);
        }

        var quote = JsonSerializer.Deserialize<SupplierQuoteMessage>(payload, SerializerOptions);
        if (quote is null)
        {
            await MarkSupplierQuoteProcessedAsync(id.Value, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogWarning("Skipped malformed local SupplierQuotes row {QueueRowId}", id.Value);
            return false;
        }

        try
        {
            await handler(quote, cancellationToken);
            await MarkSupplierQuoteProcessedAsync(id.Value, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task MarkOrderProcessedAsync(long id, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE "OrdersTopic"
            SET "ProcessedAtUtc" = NOW()
            WHERE "Id" = @id
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkSupplierQuoteProcessedAsync(long id, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE "SupplierQuotes"
            SET "ProcessedAtUtc" = NOW()
            WHERE "Id" = @id
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureTablesAsync(CancellationToken cancellationToken)
    {
        if (_tablesEnsured)
        {
            return;
        }

        await EnsureTablesSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_tablesEnsured)
            {
                return;
            }

            await using var connection = new NpgsqlConnection(ResolveConnectionString());
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(
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
                connection);

            await command.ExecuteNonQueryAsync(cancellationToken);
            _tablesEnsured = true;
        }
        finally
        {
            EnsureTablesSemaphore.Release();
        }
    }

    private string ResolveConnectionString()
    {
        var fromLocalQueue = configuration["MessageQueue:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(fromLocalQueue))
        {
            return fromLocalQueue;
        }

        fromLocalQueue = configuration["LocalQueue:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(fromLocalQueue))
        {
            return fromLocalQueue;
        }

        var fromDefaultConnection = configuration["ConnectionStrings:OrderServiceDb"];
        if (!string.IsNullOrWhiteSpace(fromDefaultConnection))
        {
            return fromDefaultConnection;
        }

        throw new InvalidOperationException("Local queue storage requires MessageQueue:ConnectionString, LocalQueue:ConnectionString, or ConnectionStrings:OrderServiceDb.");
    }

    private IReadOnlyList<string> ResolveSupplierCodes()
    {
        var fromConfiguration = configuration.GetSection("MessageQueue:SupplierCodes").Get<string[]>();
        if (fromConfiguration is not { Length: > 0 })
        {
            fromConfiguration = configuration.GetSection("LocalQueue:SupplierCodes").Get<string[]>();
        }

        if (fromConfiguration is { Length: > 0 })
        {
            return fromConfiguration
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return new[] { "ENG", "SCO", "WAL" };
    }
}
