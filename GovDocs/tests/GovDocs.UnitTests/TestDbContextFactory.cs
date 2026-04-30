using GovDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GovDocs.UnitTests;

internal static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
