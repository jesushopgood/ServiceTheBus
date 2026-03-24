using SupplierSCO.Domain.Entities;

namespace SupplierSCO.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);
}
