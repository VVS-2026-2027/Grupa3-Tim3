using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cvjecara_Latica.Tests.AdelTest;

[TestClass]
public class DiscountsControllerTests
{
    private static DiscountsController CreateController(DbContextOptions<ApplicationDbContext> options) =>
        new(new ApplicationDbContext(options));

    // Zamjenski objekat: SaveChangesAsync baca DbUpdateConcurrencyException, ostalo radi nad bazom u memoriji.
    private static Mock<ApplicationDbContext> CreateContextThrowingConcurrency(DbContextOptions<ApplicationDbContext> options)
    {
        var mock = new Mock<ApplicationDbContext>(options) { CallBase = true };
        mock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        return mock;
    }

    // ---------- Index ----------

    [TestMethod]
    public async Task Index_DiscountsExist_ReturnsViewWithAllDiscounts()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewDiscount(1, "AAA"), TestEntities.NewDiscount(2, "BBB"));
        var controller = CreateController(options);

        var result = await controller.Index();

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.IsInstanceOfType<IEnumerable<Discount>>(view.Model);
        var model = (IEnumerable<Discount>)view.Model!;
        Assert.AreEqual(2, model.Count());
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
    public async Task Details_ExistingId_ReturnsViewWithDiscount()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewDiscount(5, "FIVE"));
        var controller = CreateController(options);

        var result = await controller.Details(5);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.IsInstanceOfType<Discount>(view.Model);
        var model = (Discount)view.Model!;
        Assert.AreEqual(5, model.DiscountID);
    }

    // ---------- Create ----------

    [TestMethod]
    public void Create_Get_ReturnsView()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = controller.Create();

        Assert.IsInstanceOfType<ViewResult>(result);
    }

    [TestMethod]
    public async Task Create_Post_ValidModel_SavesAndRedirectsToIndex()
    {
        var options = TestDb.NewOptions();
        var controller = CreateController(options);

        var result = await controller.Create(TestEntities.NewDiscount(1));

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var redirect = (RedirectToActionResult)result!;
        Assert.AreEqual("Index", redirect.ActionName);
        using var verify = new ApplicationDbContext(options);
        Assert.AreEqual(1, verify.Discounts.Count());
    }

    [TestMethod]
    public async Task Create_Post_InvalidModel_ReturnsViewAndDoesNotSave()
    {
        var options = TestDb.NewOptions();
        var controller = CreateController(options);
        controller.ModelState.AddModelError("DiscountCode", "Invalid");
        var discount = TestEntities.NewDiscount(1);

        var result = await controller.Create(discount);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreSame(discount, view.Model);
        using var verify = new ApplicationDbContext(options);
        Assert.IsEmpty(verify.Discounts);
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
    public async Task Edit_Get_ExistingId_ReturnsViewWithDiscount()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewDiscount(3));
        var controller = CreateController(options);

        var result = await controller.Edit(3);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreEqual(3, ((Discount)view.Model!).DiscountID);
    }

    // ---------- Edit (POST) ----------

    [TestMethod]
    public async Task Edit_Post_IdDoesNotMatchModel_ReturnsNotFound()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = await controller.Edit(2, TestEntities.NewDiscount(1));

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Edit_Post_ValidModel_UpdatesAndRedirectsToIndex()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewDiscount(1, "OLD-CODE"));
        var controller = CreateController(options);

        var result = await controller.Edit(1, TestEntities.NewDiscount(1, "NEW-CODE"));

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var redirect = (RedirectToActionResult)result!;
        Assert.AreEqual("Index", redirect.ActionName);
        using var verify = new ApplicationDbContext(options);
        Assert.AreEqual("NEW-CODE", verify.Discounts.Single().DiscountCode);
    }

    [TestMethod]
    public async Task Edit_Post_InvalidModel_ReturnsViewWithModel()
    {
        var controller = CreateController(TestDb.NewOptions());
        controller.ModelState.AddModelError("DiscountCode", "Invalid");
        var discount = TestEntities.NewDiscount(1);

        var result = await controller.Edit(1, discount);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreSame(discount, view.Model);
    }

    [TestMethod]
    public async Task Edit_Post_ConcurrencyExceptionAndEntityDeleted_ReturnsNotFound()
    {
        var options = TestDb.NewOptions();   // baza je prazna: zapis više ne postoji
        var mockContext = CreateContextThrowingConcurrency(options);
        var controller = new DiscountsController(mockContext.Object);

        var result = await controller.Edit(1, TestEntities.NewDiscount(1));

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Edit_Post_ConcurrencyExceptionAndEntityStillExists_RethrowsException()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewDiscount(1));
        var mockContext = CreateContextThrowingConcurrency(options);
        var controller = new DiscountsController(mockContext.Object);

        await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(
            () => controller.Edit(1, TestEntities.NewDiscount(1, "NEW-CODE")));
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
    public async Task Delete_Get_ExistingId_ReturnsViewWithDiscount()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewDiscount(4));
        var controller = CreateController(options);

        var result = await controller.Delete(4);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreEqual(4, ((Discount)view.Model!).DiscountID);
    }

    // ---------- DeleteConfirmed ----------

    [TestMethod]
    public async Task DeleteConfirmed_ExistingId_RemovesDiscountAndRedirects()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewDiscount(1));
        var controller = CreateController(options);

        var result = await controller.DeleteConfirmed(1);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var redirect = (RedirectToActionResult)result!;
        Assert.AreEqual("Index", redirect.ActionName);
        using var verify = new ApplicationDbContext(options);
        Assert.IsEmpty(verify.Discounts);
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
