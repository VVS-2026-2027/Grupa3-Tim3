using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Cvjecara_Latica.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Cvjecara_Latica.Tests.HarisTest;

[TestClass]
[TestCategory("Haris")]
public partial class HomeControllerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<UserManager<Person>> _users = null!;
    private Mock<SignInManager<Person>> _signIn = null!;
    private Mock<IEmailService> _email = null!;
    private HomeController _controller = null!;

    [TestInitialize]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _context = new ApplicationDbContext(options);
        _users = new Mock<UserManager<Person>>(
            Mock.Of<IUserStore<Person>>(), Options.Create(new IdentityOptions()),
            new PasswordHasher<Person>(), Array.Empty<IUserValidator<Person>>(),
            Array.Empty<IPasswordValidator<Person>>(), new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<Person>>.Instance);
        _signIn = new Mock<SignInManager<Person>>(
            _users.Object, new HttpContextAccessor(), Mock.Of<IUserClaimsPrincipalFactory<Person>>(),
            Options.Create(new IdentityOptions()), NullLogger<SignInManager<Person>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<Person>>());
        _email = new Mock<IEmailService>();
        _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _controller = new HomeController(NullLogger<HomeController>.Instance,
            _users.Object, _signIn.Object, _context, _email.Object);
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var tempData = new Mock<ITempDataProvider>();
        tempData.Setup(p => p.LoadTempData(It.IsAny<HttpContext>()))
            .Returns(new Dictionary<string, object>());
        _controller.TempData = new TempDataDictionary(httpContext, tempData.Object);
    }

    [TestCleanup]
    public void TearDown()
    {
        _controller.Dispose();
        _context.Dispose();
        _users.Object.Dispose();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task Login_MissingEmail_ReturnsRequiredMessageWithoutCallingIdentity(string? email)
    {
        // Arrange: prazan email, bez poziva Identity servisu.
        // Act
        var result = await _controller.Login(email!, "Password1!");

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual("Email is required.", _controller.ViewData["Message"]);
        _users.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _signIn.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Login_UnknownEmail_ReturnsAccountDoesNotExistMessage()
    {
        // Arrange
        _users.Setup(u => u.FindByEmailAsync("unknown@example.com")).ReturnsAsync((Person?)null);

        // Act
        var result = await _controller.Login("unknown@example.com", "Password1!");

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual("This account does not exist.", _controller.ViewData["Message"]);
        _users.Verify(u => u.FindByEmailAsync("unknown@example.com"), Times.Once);
        _signIn.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Login_UnconfirmedEmail_RejectsLoginBeforeCheckingPassword()
    {
        // Arrange
        var user = ExistingUser(false);
        _users.Setup(u => u.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        // Act
        var result = await _controller.Login(user.Email!, "Password1!");

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual("Please verify your email address before logging in.", _controller.ViewData["Message"]);
        _signIn.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Login_ValidCredentials_RedirectsToIndex()
    {
        // Arrange
        var user = ExistingUser(true);
        _users.Setup(u => u.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _signIn.Setup(s => s.PasswordSignInAsync(user.Email!, "Password1!", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.Login(user.Email!, "Password1!");

        // Assert
        Assert.AreEqual("Index", ((RedirectToActionResult)result).ActionName);
        _signIn.Verify(s => s.PasswordSignInAsync(user.Email!, "Password1!", false, false), Times.Once);
    }

    [TestMethod]
    [DataRow("Failed")]
    [DataRow("LockedOut")]
    [DataRow("NotAllowed")]
    [DataRow("TwoFactorRequired")]
    public async Task Login_UnsuccessfulSignIn_ReturnsInvalidLoginMessage(string status)
    {
        // Arrange
        var user = ExistingUser(true);
        var signInResult = status switch
        {
            "LockedOut" => Microsoft.AspNetCore.Identity.SignInResult.LockedOut,
            "NotAllowed" => Microsoft.AspNetCore.Identity.SignInResult.NotAllowed,
            "TwoFactorRequired" => Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired,
            _ => Microsoft.AspNetCore.Identity.SignInResult.Failed
        };
        _users.Setup(u => u.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _signIn.Setup(s => s.PasswordSignInAsync(user.Email!, "wrong", false, false)).ReturnsAsync(signInResult);

        // Act
        var result = await _controller.Login(user.Email!, "wrong");

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual("Invalid login attempt.", _controller.ViewData["Message"]);
        _signIn.Verify(s => s.PasswordSignInAsync(user.Email!, "wrong", false, false), Times.Once);
    }

    [TestMethod]
    public async Task Login_IdentityThrows_PropagatesException()
    {
        // Arrange
        var error = new InvalidOperationException("Identity unavailable");
        _users.Setup(u => u.FindByEmailAsync("haris@example.com")).ThrowsAsync(error);

        // Act
        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _controller.Login("haris@example.com", "Password1!"));

        // Assert: kontroler nema catch blok.
        Assert.AreSame(error, actual);
        _signIn.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Register_ConfirmedExistingEmail_ReturnsModelAndDuplicateEmailError()
    {
        // Arrange
        var model = ValidRegistration();
        _users.Setup(u => u.FindByEmailAsync(model.Email)).ReturnsAsync(ExistingUser(true));

        // Act
        var result = await _controller.Register(model);

        // Assert
        Assert.AreSame(model, ((ViewResult)result).Model);
        Assert.AreEqual("This email is already in use.", _controller.ModelState[""]!.Errors.Single().ErrorMessage);
        VerifyNoRegistrationSideEffects();
    }

    [TestMethod]
    public async Task Register_UnconfirmedExistingEmail_ResendsCodeWithoutCreatingUserOrDiscount()
    {
        // Arrange
        var model = ValidRegistration();
        _users.Setup(u => u.FindByEmailAsync(model.Email)).ReturnsAsync(ExistingUser(false));
        var before = DateTime.UtcNow;

        // Act
        var result = await _controller.Register(model);

        // Assert
        Assert.AreEqual("VerifyCode", ((RedirectToActionResult)result).ActionName);
        var code = AssertVerificationData(model.Email, before);
        _email.Verify(e => e.SendEmailAsync(model.Email, "Email Verification Code",
            $"<p>Your new verification code is: <strong>{code}</strong></p>"), Times.Once);
        _email.VerifyNoOtherCalls();
        _users.Verify(u => u.CreateAsync(It.IsAny<Person>(), It.IsAny<string>()), Times.Never);
        Assert.AreEqual(0, _context.Discounts.Count());
        Assert.AreEqual("This email is already registered but not verified. A new code has been sent.",
            _controller.ViewData["Message"]);
    }

    [TestMethod]
    public async Task Register_InvalidModelState_ReturnsSameModelWithoutCreatingUser()
    {
        // Arrange: MVC validacija se ne izvršava automatski pri direktnom pozivu akcije.
        var model = ValidRegistration();
        _controller.ModelState.AddModelError(nameof(Register.Email), "Invalid email format.");

        // Act
        var result = await _controller.Register(model);

        // Assert
        Assert.AreSame(model, ((ViewResult)result).Model);
        Assert.IsFalse(_controller.ModelState.IsValid);
        _users.Verify(u => u.FindByEmailAsync(model.Email), Times.Once);
        VerifyNoRegistrationSideEffects();
    }

    [TestMethod]
    public async Task Register_IdentityCreationFails_ReturnsAllIdentityErrors()
    {
        // Arrange
        var model = ValidRegistration();
        _users.Setup(u => u.CreateAsync(It.IsAny<Person>(), model.Password)).ReturnsAsync(
            IdentityResult.Failed(new IdentityError { Description = "Password rejected." },
                new IdentityError { Description = "Username rejected." }));

        // Act
        var result = await _controller.Register(model);

        // Assert
        Assert.AreSame(model, ((ViewResult)result).Model);
        CollectionAssert.AreEquivalent(new[] { "Password rejected.", "Username rejected." },
            _controller.ModelState[""]!.Errors.Select(e => e.ErrorMessage).ToArray());
        _users.Verify(u => u.CreateAsync(It.IsAny<Person>(), model.Password), Times.Once);
        Assert.AreEqual(0, _context.Discounts.Count());
        _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Register_ValidNewUser_CreatesUserDiscountAndTwoEmails()
    {
        // Arrange
        var model = ValidRegistration();
        Person? createdUser = null;
        _users.Setup(u => u.CreateAsync(It.IsAny<Person>(), model.Password))
            .Callback<Person, string>((user, _) => createdUser = user).ReturnsAsync(IdentityResult.Success);
        var beforeUtc = DateTime.UtcNow;
        var beforeLocal = DateTime.Now;

        // Act
        var result = await _controller.Register(model);

        // Assert
        Assert.AreEqual("VerifyCode", ((RedirectToActionResult)result).ActionName);
        Assert.IsNotNull(createdUser);
        Assert.AreEqual(model.Email, createdUser.Email);
        Assert.AreEqual(model.Email, createdUser.UserName);
        Assert.AreEqual(model.FirstName, createdUser.FirstName);
        Assert.AreEqual(model.LastName, createdUser.LastName);
        Assert.AreEqual(model.HomeAdress, createdUser.HomeAdress);
        Assert.AreEqual(model.PhoneNumber, createdUser.PhoneNumber);
        Assert.IsFalse(createdUser.EmailConfirmed);
        var code = AssertVerificationData(model.Email, beforeUtc);
        var discount = _context.Discounts.Single();
        Assert.AreEqual(createdUser.Id, discount.PersonID);
        Assert.AreEqual(10.0, discount.DiscountAmount);
        Assert.AreEqual(DiscountType.PercentageOff, discount.DiscountType);
        StringAssert.Matches(discount.DiscountCode, new System.Text.RegularExpressions.Regex("^PROMO-[A-F0-9]{8}$"));
        Assert.IsTrue(discount.DiscountBegins >= beforeLocal && discount.DiscountBegins <= DateTime.Now);
        Assert.IsFalse(discount.IsUsed);
        _users.Verify(u => u.CreateAsync(It.IsAny<Person>(), model.Password), Times.Once);
        _email.Verify(e => e.SendEmailAsync(model.Email, "Email Verification Code",
            $"<p>Your verification code is: <strong>{code}</strong></p>"), Times.Once);
        _email.Verify(e => e.SendEmailAsync(model.Email, "Welcome! Your promo code",
            $"<p>Thank you for registering! Your promo code for a 10% discount is: <strong>{discount.DiscountCode}</strong></p>"), Times.Once);
        _email.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Register_VerificationEmailThrows_PropagatesExceptionBeforeSavingDiscount()
    {
        // Arrange
        var model = ValidRegistration();
        _users.Setup(u => u.CreateAsync(It.IsAny<Person>(), model.Password)).ReturnsAsync(IdentityResult.Success);
        var error = new InvalidOperationException("SMTP unavailable");
        _email.Setup(e => e.SendEmailAsync(model.Email, "Email Verification Code", It.IsAny<string>()))
            .ThrowsAsync(error);

        // Act
        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _controller.Register(model));

        // Assert: korisnik je kreiran prije slanja; nema transakcijskog vraćanja.
        Assert.AreSame(error, actual);
        _users.Verify(u => u.CreateAsync(It.IsAny<Person>(), model.Password), Times.Once);
        Assert.AreEqual(0, _context.Discounts.Count());
        _email.Verify(e => e.SendEmailAsync(model.Email, "Welcome! Your promo code", It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Logout_SignedInUser_SignsOutAndRedirectsToHomeIndex()
    {
        // Arrange
        _signIn.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

        // Act
        var result = (RedirectToActionResult)await _controller.Logout();

        // Assert
        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual("Home", result.ControllerName);
        _signIn.Verify(s => s.SignOutAsync(), Times.Once);
    }

    internal static Register ValidRegistration() => new()
    {
        FirstName = "Haris", LastName = "Test", HomeAdress = "Test ulica 1",
        Email = "haris@example.com", PhoneNumber = "+38761123456",
        Password = "Password1!", ConfirmPassword = "Password1!"
    };

    private static Person ExistingUser(bool confirmed) => new()
    {
        Id = "user-1", Email = "haris@example.com", UserName = "haris@example.com",
        EmailConfirmed = confirmed, FirstName = "Haris", LastName = "Test", HomeAdress = "Test ulica 1"
    };

    private string AssertVerificationData(string email, DateTime before)
    {
        var code = (string)_controller.TempData.Peek("VerificationCode")!;
        StringAssert.Matches(code, new System.Text.RegularExpressions.Regex("^[0-9]{6}$"));
        Assert.AreEqual(email, _controller.TempData.Peek("UserEmail"));
        var generatedAt = (DateTime)_controller.TempData.Peek("CodeGeneratedAt")!;
        Assert.IsTrue(generatedAt >= before && generatedAt <= DateTime.UtcNow);
        return code;
    }

    private void VerifyNoRegistrationSideEffects()
    {
        _users.Verify(u => u.CreateAsync(It.IsAny<Person>(), It.IsAny<string>()), Times.Never);
        _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.AreEqual(0, _context.Discounts.Count());
    }
}
