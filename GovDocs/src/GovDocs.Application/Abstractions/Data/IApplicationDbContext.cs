using GovDocs.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace GovDocs.Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
