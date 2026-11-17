using Cvjecara_Latica.Data;
using Microsoft.EntityFrameworkCore;

namespace Cvjecara_Latica.Tests.Helpers
{
    /// <summary>
    /// Pravi zamjensku bazu u memoriji (InMemory) za testove.
    /// Svaki poziv daje novu, praznu bazu, pa su testovi međusobno nezavisni.
    /// </summary>
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext Kreiraj()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(opcije);
        }
    }
}
