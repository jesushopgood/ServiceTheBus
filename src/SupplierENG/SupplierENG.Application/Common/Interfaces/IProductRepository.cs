using SupplierENG.Domain.Entities;

namespace SupplierENG.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);
}
