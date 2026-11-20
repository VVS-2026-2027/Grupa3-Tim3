using Cvjecara_Latica.Data;
using Microsoft.EntityFrameworkCore;

namespace Cvjecara_Latica.Tests.AdelTest;

public static class TestDb
{
    // Svaki test dobija svoju bazu u memoriji (jedinstveno ime) pa su testovi međusobno neovisni.
    public static DbContextOptions<ApplicationDbContext> NewOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    // Upisuje početne podatke preko zasebnog contexta, da kontroler dobije "čist" context.
    public static void Seed(DbContextOptions<ApplicationDbContext> options, params object[] entities)
    {
        using var context = new ApplicationDbContext(options);
        context.AddRange(entities);
        context.SaveChanges();
    }
}
