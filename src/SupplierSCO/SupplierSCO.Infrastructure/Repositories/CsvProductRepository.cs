using System.Globalization;
using Microsoft.Extensions.Options;
using SupplierSCO.Application.Common.Interfaces;
using SupplierSCO.Domain.Entities;
using SupplierSCO.Infrastructure.Messaging;

namespace SupplierSCO.Infrastructure.Repositories;

public sealed class CsvProductRepository(IOptions<SupplierServiceBusOptions> options) : IProductRepository
{
    private readonly SupplierServiceBusOptions _options = options.Value;

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        var path = ResolveCsvPath();
        var products = new List<Product>();

        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);

        var lineNumber = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            lineNumber++;
            if (lineNumber == 1 && line.StartsWith("Id,", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var values = line.Split(',', 4, StringSplitOptions.TrimEntries);
            if (values.Length != 4)
            {
                continue;
            }

            if (!int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }

            if (!decimal.TryParse(values[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var bestPrice))
            {
                continue;
            }

            products.Add(new Product
            {
                Id = id,
                Sku = values[1],
                Description = values[2],
                BestPrice = bestPrice
            });
        }

        return products;
    }

    private string ResolveCsvPath()
    {
        var attemptedPaths = new List<string>();

        if (!string.IsNullOrWhiteSpace(_options.ProductsCsvPath))
        {
            var configured = Path.IsPathRooted(_options.ProductsCsvPath)
                ? _options.ProductsCsvPath
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), _options.ProductsCsvPath));

            attemptedPaths.Add(configured);

            if (File.Exists(configured))
            {
                return configured;
            }
        }

        var fallbackPaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "products.csv"),
            Path.Combine(AppContext.BaseDirectory, "products.csv")
        };

        foreach (var fallbackPath in fallbackPaths)
        {
            attemptedPaths.Add(fallbackPath);
            if (File.Exists(fallbackPath))
            {
                return fallbackPath;
            }
        }

        throw new FileNotFoundException($"Supplier products.csv not found. Check SupplierServiceBus:ProductsCsvPath. Attempted: {string.Join("; ", attemptedPaths)}");
    }
}
