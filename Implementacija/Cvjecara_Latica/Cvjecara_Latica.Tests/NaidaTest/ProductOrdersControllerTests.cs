
using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cvjecara_Latica.Tests.NaidaTest;

[TestClass]
public class ProductOrdersControllerTests
{
    [TestMethod]
    public async Task Index_PraznaBaza_VracaPraznuListu()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        var rezultat = await controller.Index();

        var view = rezultat as ViewResult;
        Assert.IsNotNull(view);

        var stavke = (IEnumerable<ProductOrder>)view.Model!;
        Assert.AreEqual(0, stavke.Count());
    }

    [TestMethod]
    public async Task Details_NullId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        var rezultat = await controller.Details(null);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Details_NepostojeciId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        var rezultat = await controller.Details(999);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public void Create_Get_VracaView()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        var rezultat = controller.Create();

        Assert.IsInstanceOfType<ViewResult>(rezultat);
    }

    [TestMethod]
    public async Task CreatePost_Ispravan_SpremaStavku()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        var stavka = NaidaTestHelpers.Stavka();

        var rezultat = await controller.Create(stavka);

        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual(1, await db.ProductOrders.CountAsync());
    }

    [TestMethod]
    public async Task CreatePost_Nevalidan_VracaView()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        controller.ModelState.AddModelError(
            "ProductQuantity", "Neispravna kolicina");

        var rezultat = await controller.Create(
            NaidaTestHelpers.Stavka());

        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(0, await db.ProductOrders.CountAsync());
    }

    [TestMethod]
    public async Task EditGet_NullId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        var rezultat = await controller.Edit((int?)null);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task EditGet_NepostojeciId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        var rezultat = await controller.Edit((int?)999);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task EditPost_PogresanId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        var rezultat = await controller.Edit(
            999, NaidaTestHelpers.Stavka());

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task EditPost_Nevalidan_VracaView()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        controller.ModelState.AddModelError(
            "ProductQuantity", "Neispravna kolicina");

        var rezultat = await controller.Edit(
            1, NaidaTestHelpers.Stavka());

        Assert.IsInstanceOfType<ViewResult>(rezultat);
    }

    [TestMethod]
    public async Task DeleteGet_NullId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        var rezultat = await controller.Delete((int?)null);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task DeleteGet_NepostojeciId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        var rezultat = await controller.Delete((int?)999);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task DeleteConfirmed_NepostojeciId_Redirect()
    {
        using var db = NaidaTestHelpers.Baza();
        var controller = new ProductOrdersController(db);

        var rezultat = await controller.DeleteConfirmed(999);

        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
    }

    [TestMethod]
    public async Task DeleteConfirmed_Postojeci_BriseStavku()
    {
        using var db = NaidaTestHelpers.Baza();

        db.ProductOrders.Add(NaidaTestHelpers.Stavka());
        await db.SaveChangesAsync();

        var controller = new ProductOrdersController(db);

        var rezultat = await controller.DeleteConfirmed(1);

        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual(0, await db.ProductOrders.CountAsync());
    }
}
