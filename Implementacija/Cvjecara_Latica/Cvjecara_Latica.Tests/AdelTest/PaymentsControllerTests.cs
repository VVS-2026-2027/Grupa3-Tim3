using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cvjecara_Latica.Tests.AdelTest;

[TestClass]
public class PaymentsControllerTests
{
    private static PaymentsController CreateController(DbContextOptions<ApplicationDbContext> options) =>
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
    public async Task Index_PaymentsExist_ReturnsViewWithPaymentsAndTheirDiscounts()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewDiscount(1), TestEntities.NewPayment(1, discountId: 1));
        var controller = CreateController(options);

        var result = await controller.Index();

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.IsInstanceOfType<IEnumerable<Payment>>(view.Model);
        var model = (IEnumerable<Payment>)view.Model!;
        Assert.HasCount(1, model);
        var payment = model.Single();
        Assert.IsNotNull(payment.Discount);
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
    public async Task Details_ExistingId_ReturnsViewWithPayment()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewPayment(7));
        var controller = CreateController(options);

        var result = await controller.Details(7);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreEqual(7, ((Payment)view.Model!).PaymentID);
    }

    // ---------- Create ----------

    [TestMethod]
    public void Create_Get_ReturnsViewWithDiscountSelectList()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = controller.Create();

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.IsInstanceOfType<SelectList>(view.ViewData["DiscountID"]);
    }

    [TestMethod]
    public async Task Create_Post_ValidModel_SavesAndRedirectsToIndex()
    {
        var options = TestDb.NewOptions();
        var controller = CreateController(options);

        var result = await controller.Create(TestEntities.NewPayment(1));

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var redirect = (RedirectToActionResult)result!;
        Assert.AreEqual("Index", redirect.ActionName);
        using var verify = new ApplicationDbContext(options);
        Assert.AreEqual(1, verify.Payments.Count());
    }

    [TestMethod]
    public async Task Create_Post_InvalidModel_ReturnsViewWithSelectListAndDoesNotSave()
    {
        var options = TestDb.NewOptions();
        var controller = CreateController(options);
        controller.ModelState.AddModelError("PayedAmount", "Invalid");
        var payment = TestEntities.NewPayment(1);

        var result = await controller.Create(payment);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreSame(payment, view.Model);
        Assert.IsInstanceOfType<SelectList>(view.ViewData["DiscountID"]);
        using var verify = new ApplicationDbContext(options);
        Assert.IsEmpty(verify.Payments);
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
    public async Task Edit_Get_ExistingId_ReturnsViewWithPaymentAndSelectList()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewPayment(3));
        var controller = CreateController(options);

        var result = await controller.Edit(3);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreEqual(3, ((Payment)view.Model!).PaymentID);
        Assert.IsInstanceOfType<SelectList>(view.ViewData["DiscountID"]);
    }

    // ---------- Edit (POST) ----------

    [TestMethod]
    public async Task Edit_Post_IdDoesNotMatchModel_ReturnsNotFound()
    {
        var controller = CreateController(TestDb.NewOptions());

        var result = await controller.Edit(2, TestEntities.NewPayment(1));

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Edit_Post_ValidModel_UpdatesAndRedirectsToIndex()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewPayment(1));
        var updated = TestEntities.NewPayment(1);
        updated.PayedAmount = 123.45;
        var controller = CreateController(options);

        var result = await controller.Edit(1, updated);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var redirect = (RedirectToActionResult)result!;
        Assert.AreEqual("Index", redirect.ActionName);
        using var verify = new ApplicationDbContext(options);
        Assert.AreEqual(123.45, verify.Payments.Single().PayedAmount);
    }

    [TestMethod]
    public async Task Edit_Post_InvalidModel_ReturnsViewWithSelectList()
    {
        var controller = CreateController(TestDb.NewOptions());
        controller.ModelState.AddModelError("PayedAmount", "Invalid");
        var payment = TestEntities.NewPayment(1);

        var result = await controller.Edit(1, payment);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreSame(payment, view.Model);
        Assert.IsInstanceOfType<SelectList>(view.ViewData["DiscountID"]);
    }

    [TestMethod]
    public async Task Edit_Post_ConcurrencyExceptionAndEntityDeleted_ReturnsNotFound()
    {
        var options = TestDb.NewOptions();
        var mockContext = CreateContextThrowingConcurrency(options);
        var controller = new PaymentsController(mockContext.Object);

        var result = await controller.Edit(1, TestEntities.NewPayment(1));

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Edit_Post_ConcurrencyExceptionAndEntityStillExists_RethrowsException()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewPayment(1));
        var mockContext = CreateContextThrowingConcurrency(options);
        var controller = new PaymentsController(mockContext.Object);

        await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(
            () => controller.Edit(1, TestEntities.NewPayment(1)));
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
    public async Task Delete_Get_ExistingId_ReturnsViewWithPayment()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewPayment(4));
        var controller = CreateController(options);

        var result = await controller.Delete(4);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result!;
        Assert.AreEqual(4, ((Payment)view.Model!).PaymentID);
    }

    // ---------- DeleteConfirmed ----------

    [TestMethod]
    public async Task DeleteConfirmed_ExistingId_RemovesPaymentAndRedirects()
    {
        var options = TestDb.NewOptions();
        TestDb.Seed(options, TestEntities.NewPayment(1));
        var controller = CreateController(options);

        var result = await controller.DeleteConfirmed(1);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var redirect = (RedirectToActionResult)result!;
        Assert.AreEqual("Index", redirect.ActionName);
        using var verify = new ApplicationDbContext(options);
        Assert.IsEmpty(verify.Payments);
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
