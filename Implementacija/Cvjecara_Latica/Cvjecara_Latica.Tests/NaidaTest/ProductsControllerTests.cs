
using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cvjecara_Latica.Tests.NaidaTest;

[TestClass]
public class ProductsControllerTests
{
    [TestMethod]
    public async Task Index_VracaSveStavke()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Products.Add(NaidaTestHelpers.Proizvod());
        await db.SaveChangesAsync();

        var rezultat = await new ProductsController(db).Index();
        var view = rezultat as ViewResult;

        Assert.IsNotNull(view);
        Assert.AreEqual(
            1, ((IEnumerable<Product>)view.Model!).Count());
    }

    [TestMethod]
    public async Task Details_NullId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        var rezultat =
            await new ProductsController(db).Details(null);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Details_NepostojeciId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        var rezultat =
            await new ProductsController(db).Details(999);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Details_PostojeciId_VracaModel()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Products.Add(NaidaTestHelpers.Proizvod());
        await db.SaveChangesAsync();

        var view =
            await new ProductsController(db).Details(1)
            as ViewResult;

        Assert.IsNotNull(view);
        Assert.AreEqual(1, ((Product)view.Model!).ProductID);
    }

    [TestMethod]
    public void CreateGet_VracaView()
    {
        using var db = NaidaTestHelpers.Baza();

        var rezultat = new ProductsController(db).Create();

        Assert.IsInstanceOfType<ViewResult>(rezultat);
    }

    [TestMethod]
    public async Task CreatePost_IspravanModel_SpremaIRedirect()
    {
        using var db = NaidaTestHelpers.Baza();
        var c = new ProductsController(db);

        var rezultat =
            await c.Create(NaidaTestHelpers.Proizvod());

        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual(1, await db.Products.CountAsync());
    }

    [TestMethod]
    public async Task CreatePost_NevalidanModel_NeSprema()
    {
        using var db = NaidaTestHelpers.Baza();
        var c = new ProductsController(db);
        c.ModelState.AddModelError("test", "Neispravno");

        var rezultat =
            await c.Create(NaidaTestHelpers.Proizvod());

        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(0, await db.Products.CountAsync());
    }

    [TestMethod]
    public async Task EditGet_NullId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        var rezultat =
            await new ProductsController(db).Edit((int?)null);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task EditGet_NepostojeciId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        var rezultat =
            await new ProductsController(db).Edit((int?)999);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task EditGet_PostojeciId_VracaModel()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Products.Add(NaidaTestHelpers.Proizvod());
        await db.SaveChangesAsync();

        var rezultat =
            await new ProductsController(db).Edit((int?)1);

        Assert.IsInstanceOfType<ViewResult>(rezultat);
    }

    [TestMethod]
    public async Task EditPost_NepodudaranId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        var rezultat =
            await new ProductsController(db)
                .Edit(2, NaidaTestHelpers.Proizvod());

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task EditPost_NevalidanModel_VracaView()
    {
        using var db = NaidaTestHelpers.Baza();
        var c = new ProductsController(db);
        c.ModelState.AddModelError("test", "Neispravno");

        var rezultat =
            await c.Edit(1, NaidaTestHelpers.Proizvod());

        Assert.IsInstanceOfType<ViewResult>(rezultat);
    }

    [TestMethod]
    public async Task EditPost_IspravanModel_Redirect()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Products.Add(NaidaTestHelpers.Proizvod());
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var rezultat =
            await new ProductsController(db)
                .Edit(1, NaidaTestHelpers.Proizvod());

        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
    }

    [TestMethod]
    public async Task DeleteGet_NullId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        var rezultat =
            await new ProductsController(db).Delete((int?)null);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task DeleteGet_NepostojeciId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        var rezultat =
            await new ProductsController(db).Delete((int?)999);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task DeleteGet_PostojeciId_VracaModel()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Products.Add(NaidaTestHelpers.Proizvod());
        await db.SaveChangesAsync();

        var rezultat =
            await new ProductsController(db).Delete((int?)1);

        Assert.IsInstanceOfType<ViewResult>(rezultat);
    }

    [TestMethod]
    public async Task DeleteConfirmed_PostojeciId_Brise()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Products.Add(NaidaTestHelpers.Proizvod());
        await db.SaveChangesAsync();

        var rezultat =
            await new ProductsController(db).DeleteConfirmed(1);

        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual(0, await db.Products.CountAsync());
    }

    [TestMethod]
    public async Task DeleteConfirmed_NepostojeciId_NeBacaIzuzetak()
    {
        using var db = NaidaTestHelpers.Baza();

        var rezultat =
            await new ProductsController(db).DeleteConfirmed(999);

        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
    }
}
