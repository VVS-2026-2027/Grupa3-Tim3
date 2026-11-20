using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cvjecara_Latica.Tests.AdelTest;

[TestClass]
public class ProductSalesControllerTests
{
    private static ProductSalesController CreateController(DbContextOptions<ApplicationDbContext> options) =>
        new(new ApplicationDbContext(options));

    private static Mock<ApplicationDbContext> CreateContextThrowingConcurrency(DbContextOptions<ApplicationDbContext> options)
    {
        var mock = new Mock<ApplicationDbContext>(options) { CallBase = true };
        mock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        return mock;
    }

    // ---------- Index ----------

    [TestMethod]
    public async Task Index_SalesExist_ReturnsViewWithSalesAndTheirProducts()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewProduct(1), TestEntities.NewProductSale(1, productId: 1));
        var controller = CreateController(options);

        var result = await controller.Index();

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.IsInstanceOfType<IEnumerable<ProductSale>>(view.Model);
        var model = (IEnumerable<ProductSale>)view.Model!;
        Assert.HasCount(1, model);
        var sale = model.Single();
        Assert.IsNotNull(sale.Product);
    }

    // ---------- Details ----------

    [TestMethod]
    public async Task Details_NullId_ReturnsNotFound()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = await controller.Details(null);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Details_UnknownId_ReturnsNotFound()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = await controller.Details(99);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Details_ExistingId_ReturnsViewWithProductSale()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewProduct(1), TestEntities.NewProductSale(6, productId: 1));
        var controller = CreateController(options);

        var result = await controller.Details(6);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreEqual(6, ((ProductSale)view.Model!).ProductSalesID);
    }

    // ---------- Create ----------

    [TestMethod]
    public void Create_Get_ReturnsViewWithProductSelectList()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = controller.Create();

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.IsInstanceOfType<SelectList>(view.ViewData["ProductID"]);
    }

    [TestMethod]
    public async Task Create_Post_ValidModel_SavesAndRedirectsToIndex()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewProduct(1));
        var controller = CreateController(options);

        var result = await controller.Create(TestEntities.NewProductSale(1, productId: 1));

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var redirect = (RedirectToActionResult)result!;
        Assert.AreEqual("Index", redirect.ActionName);
        using var verify = new ApplicationDbContext(options);
        Assert.AreEqual(1, verify.ProductSales.Count());
    }

    [TestMethod]
    public async Task Create_Post_InvalidModel_ReturnsViewWithSelectListAndDoesNotSave()
    {
        var options = TestDb.NewOptions();
        var controller = CreateController(options);
        controller.ModelState.AddModelError("SalesDate", "Invalid");
        var sale = TestEntities.NewProductSale(1);

        var result = await controller.Create(sale);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreSame(sale, view.Model);
        Assert.IsInstanceOfType<SelectList>(view.ViewData["ProductID"]);
        using var verify = new ApplicationDbContext(options);
        Assert.IsEmpty(verify.ProductSales);
    }

    // ---------- Edit (GET) ----------

    [TestMethod]
    public async Task Edit_Get_NullId_ReturnsNotFound()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = await controller.Edit(null);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Edit_Get_UnknownId_ReturnsNotFound()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = await controller.Edit(99);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Edit_Get_ExistingId_ReturnsViewWithProductSaleAndSelectList()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewProduct(1), TestEntities.NewProductSale(3, productId: 1));
        var controller = CreateController(options);

        var result = await controller.Edit(3);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreEqual(3, ((ProductSale)view.Model!).ProductSalesID);
        Assert.IsInstanceOfType<SelectList>(view.ViewData["ProductID"]);
    }

    // ---------- Edit (POST) ----------

    [TestMethod]
    public async Task Edit_Post_IdDoesNotMatchModel_ReturnsNotFound()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = await controller.Edit(2, TestEntities.NewProductSale(1));

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Edit_Post_ValidModel_UpdatesAndRedirectsToIndex()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewProduct(1), TestEntities.NewProduct(2), TestEntities.NewProductSale(1, productId: 1));
        var controller = CreateController(options);

        var result = await controller.Edit(1, TestEntities.NewProductSale(1, productId: 2));

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var redirect = (RedirectToActionResult)result!;
        Assert.AreEqual("Index", redirect.ActionName);
        using var verify = new ApplicationDbContext(options);
        Assert.AreEqual(2, verify.ProductSales.Single().ProductID);
    }

    [TestMethod]
    public async Task Edit_Post_InvalidModel_ReturnsViewWithSelectList()
    {
        var controller = CreateController(TestDb.NewOptions());
        controller.ModelState.AddModelError("SalesDate", "Invalid");
        var sale = TestEntities.NewProductSale(1);

        var result = await controller.Edit(1, sale);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreSame(sale, view.Model);
        Assert.IsInstanceOfType<SelectList>(view.ViewData["ProductID"]);
    }

    [TestMethod]
    public async Task Edit_Post_ConcurrencyExceptionAndEntityDeleted_ReturnsNotFound()
    {
        var options = TestDb.NewOptions();
        var mockContext = CreateContextThrowingConcurrency(options);
        var controller = new ProductSalesController(mockContext.Object);

        var result = await controller.Edit(1, TestEntities.NewProductSale(1));

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Edit_Post_ConcurrencyExceptionAndEntityStillExists_RethrowsException()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewProduct(1), TestEntities.NewProductSale(1, productId: 1));
        var mockContext = CreateContextThrowingConcurrency(options);
        var controller = new ProductSalesController(mockContext.Object);

        await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(
            () => controller.Edit(1, TestEntities.NewProductSale(1, productId: 1)));
    }

    // ---------- Delete (GET) ----------

    [TestMethod]
    public async Task Delete_Get_NullId_ReturnsNotFound()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = await controller.Delete(null);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Delete_Get_UnknownId_ReturnsNotFound()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = await controller.Delete(99);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Delete_Get_ExistingId_ReturnsViewWithProductSale()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewProduct(1), TestEntities.NewProductSale(4, productId: 1));
        var controller = CreateController(options);

        var result = await controller.Delete(4);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreEqual(4, ((ProductSale)view.Model!).ProductSalesID);
    }

    // ---------- DeleteConfirmed ----------

    [TestMethod]
    public async Task DeleteConfirmed_ExistingId_RemovesProductSaleAndRedirects()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewProduct(1), TestEntities.NewProductSale(1, productId: 1));
        var controller = CreateController(options);

        var result = await controller.DeleteConfirmed(1);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var redirect = (RedirectToActionResult)result!;
        Assert.AreEqual("Index", redirect.ActionName);
        using var verify = new ApplicationDbContext(options);
        Assert.IsEmpty(verify.ProductSales);
    }

    [TestMethod]
    public async Task DeleteConfirmed_UnknownId_StillRedirectsToIndex()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = await controller.DeleteConfirmed(99);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var redirect = (RedirectToActionResult)result!;
        Assert.AreEqual("Index", redirect.ActionName);
    }
}
