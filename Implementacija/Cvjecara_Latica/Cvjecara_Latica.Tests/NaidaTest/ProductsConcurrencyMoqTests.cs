
using System;
using System.Threading;
using System.Threading.Tasks;
using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Cvjecara_Latica.Tests.NaidaTest;

[TestClass]
public class ProductsConcurrencyMoqTests
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
    public async Task Edit_KadaProizvodNePostoji_VracaNotFound()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new BazaSaGreskom(options);

        var controller = new ProductsController(db);
        var product = NaidaTestHelpers.Proizvod(1);

        var rezultat = await controller.Edit(1, product);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Edit_KadaProizvodPostoji_PonovoBacaIzuzetak()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using (var db = new ApplicationDbContext(options))
        {
            db.Products.Add(NaidaTestHelpers.Proizvod(1));
            await db.SaveChangesAsync();
        }

        using var dbSaGreskom = new BazaSaGreskom(options);

        var controller = new ProductsController(dbSaGreskom);
        var product = NaidaTestHelpers.Proizvod(1);

        
await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
    async () => await controller.Edit(1, product));

    }
}
