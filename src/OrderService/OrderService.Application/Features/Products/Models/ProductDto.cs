namespace OrderService.Application.Features.Products.Models;

public sealed record ProductDto(string Sku, string Description, decimal BasePrice);
