
using System;
using System.Threading;
using System.Threading.Tasks;
using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cvjecara_Latica.Tests.NaidaTest;

[TestClass]
public class ProductOrdersConcurrencyTests
{
    private class BazaSaGreskom : ApplicationDbContext
    {
        public BazaSaGreskom(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            throw new DbUpdateConcurrencyException(
                "Simulirana greska konkurentnosti.");
        }
    }

    [TestMethod]
    public async Task Edit_KadaStavkaNePostoji_VracaNotFound()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new BazaSaGreskom(options);

        var controller = new ProductOrdersController(db);
        var stavka = NaidaTestHelpers.Stavka(1);

        var rezultat = await controller.Edit(1, stavka);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Edit_KadaStavkaPostoji_PonovoBacaIzuzetak()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using (var db = new ApplicationDbContext(options))
        {
            db.ProductOrders.Add(NaidaTestHelpers.Stavka(1));
            await db.SaveChangesAsync();
        }

        using var dbSaGreskom = new BazaSaGreskom(options);

        var controller = new ProductOrdersController(dbSaGreskom);
        var stavka = NaidaTestHelpers.Stavka(1);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await controller.Edit(1, stavka));
    }
}
