using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Cvjecara_Latica.Models;
using Moq;

namespace Cvjecara_Latica.Tests.HarisTest;

public partial class HomeControllerTests
{
    [TestMethod]
    public void VerifyCode_Get_ReturnsViewAndRetainsVerificationData()
    {
        // Arrange: čitanje označava TempData za brisanje na kraju zahtjeva.
        SetVerificationData();
        _ = _controller.TempData["VerificationCode"];
        _ = _controller.TempData["UserEmail"];
        _ = _controller.TempData["CodeGeneratedAt"];

        // Act
        var result = _controller.VerifyCode();
        _controller.TempData.Save();

        // Assert: Keep zadržava prethodno pročitane podatke.
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual("123456", _controller.TempData.Peek("VerificationCode"));
        Assert.AreEqual("haris@example.com", _controller.TempData.Peek("UserEmail"));
        Assert.IsNotNull(_controller.TempData.Peek("CodeGeneratedAt"));
    }

    [TestMethod]
    [DataRow("VerificationCode")]
    [DataRow("UserEmail")]
    [DataRow("CodeGeneratedAt")]
    public async Task VerifyCode_MissingSessionData_ReturnsRegisterAgainError(string missingKey)
    {
        // Arrange
        SetVerificationData();
        _controller.TempData.Remove(missingKey);

        // Act
        var result = await _controller.VerifyCode("123456");

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual("Code has expired. Please register again.", _controller.ViewData["Error"]);
        _users.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task VerifyCode_AllSessionDataMissing_ReturnsRegisterAgainError()
    {
        // Arrange
        SetVerificationData();
        _controller.TempData.Clear();

        // Act
        var result = await _controller.VerifyCode("123456");

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual("Code has expired. Please register again.", _controller.ViewData["Error"]);
        _users.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task VerifyCode_CodeOlderThanTwoMinutes_ReturnsExpiredError()
    {
        // Arrange: 121 s izbjegava nestabilnu provjeru tačno na granici isteka.
        SetVerificationData(DateTime.UtcNow.AddSeconds(-121));

        // Act
        var result = await _controller.VerifyCode("123456");

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual("Code expired. Please request a new one.", _controller.ViewData["Error"]);
        _users.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        Assert.AreEqual("123456", _controller.TempData.Peek("VerificationCode"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("000000")]
    [DataRow(" 123456 ")]
    public async Task VerifyCode_IncorrectCode_ReturnsErrorWithoutUpdatingUser(string? code)
    {
        // Arrange
        SetVerificationData();

        // Act
        var result = await _controller.VerifyCode(code!);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual("The code you entered is incorrect.", _controller.ViewData["Error"]);
        _users.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _users.Verify(u => u.UpdateAsync(It.IsAny<Person>()), Times.Never);
    }

    [TestMethod]
    public async Task VerifyCode_CorrectCodeWithinTwoMinutes_ConfirmsEmailAndReturnsSuccessView()
    {
        // Arrange: pet sekundi prije isteka, uz prostor za vrijeme izvršavanja testa.
        SetVerificationData(DateTime.UtcNow.AddSeconds(-115));
        var user = ExistingUser(false);
        _users.Setup(u => u.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _users.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = (ViewResult)await _controller.VerifyCode("123456");
        _controller.TempData.Save();

        // Assert
        Assert.AreEqual("VerificationSuccess", result.ViewName);
        Assert.IsTrue(user.EmailConfirmed);
        _users.Verify(u => u.UpdateAsync(user), Times.Once);
        Assert.AreEqual("123456", _controller.TempData.Peek("VerificationCode"));
        Assert.AreEqual(user.Email, _controller.TempData.Peek("UserEmail"));
        Assert.IsNotNull(_controller.TempData.Peek("CodeGeneratedAt"));
    }

    [TestMethod]
    public async Task VerifyCode_AlreadyConfirmedUser_ReturnsSuccessWithoutUpdatingUser()
    {
        // Arrange
        SetVerificationData();
        var user = ExistingUser(true);
        _users.Setup(u => u.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        // Act
        var result = (ViewResult)await _controller.VerifyCode("123456");

        // Assert
        Assert.AreEqual("VerificationSuccess", result.ViewName);
        Assert.IsTrue(user.EmailConfirmed);
        _users.Verify(u => u.UpdateAsync(It.IsAny<Person>()), Times.Never);
    }

    [TestMethod]
    public async Task VerifyCode_UnknownUserWithCorrectCode_ReturnsSuccessWithoutUpdatingUser()
    {
        // Arrange: dokumentuje nedostatak implementacije, koja i za nepostojećeg korisnika vraća uspjeh.
        SetVerificationData();
        _users.Setup(u => u.FindByEmailAsync("haris@example.com")).ReturnsAsync((Person?)null);

        // Act
        var result = (ViewResult)await _controller.VerifyCode("123456");

        // Assert
        Assert.AreEqual("VerificationSuccess", result.ViewName);
        _users.Verify(u => u.FindByEmailAsync("haris@example.com"), Times.Once);
        _users.Verify(u => u.UpdateAsync(It.IsAny<Person>()), Times.Never);
    }

    [TestMethod]
    public async Task VerifyCode_IdentityUpdateFails_StillReturnsSuccessView()
    {
        // Arrange: trenutni kod ignoriše IdentityResult; ovo nije dokaz uspješnog upisa u bazu.
        SetVerificationData();
        var user = ExistingUser(false);
        _users.Setup(u => u.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _users.Setup(u => u.UpdateAsync(user)).ReturnsAsync(
            IdentityResult.Failed(new IdentityError { Description = "Update failed." }));

        // Act
        var result = (ViewResult)await _controller.VerifyCode("123456");

        // Assert
        Assert.AreEqual("VerificationSuccess", result.ViewName);
        _users.Verify(u => u.UpdateAsync(user), Times.Once);
        Assert.IsTrue(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task VerifyCode_MalformedStoredTimestamp_PropagatesFormatException()
    {
        // Arrange
        SetVerificationData();
        _controller.TempData["CodeGeneratedAt"] = "not-a-date";

        // Act
        await Assert.ThrowsExactlyAsync<FormatException>(() => _controller.VerifyCode("123456"));

        // Assert
        _users.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ResendCode_MissingEmail_RedirectsToRegisterWithoutSendingEmail()
    {
        // Arrange: TempData je prazan.
        // Act
        var result = (RedirectToActionResult)await _controller.ResendCode();

        // Assert
        Assert.AreEqual("Register", result.ActionName);
        _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.IsNull(_controller.TempData.Peek("VerificationCode"));
    }

    [TestMethod]
    public async Task ResendCode_ExistingEmail_ReplacesExpiredDataAndSendsNewCode()
    {
        // Arrange
        SetVerificationData(DateTime.UtcNow.AddMinutes(-10));
        _controller.TempData["VerificationCode"] = "expired";
        var before = DateTime.UtcNow;

        // Act
        var result = (RedirectToActionResult)await _controller.ResendCode();
        _controller.TempData.Save();

        // Assert
        Assert.AreEqual("VerifyCode", result.ActionName);
        var code = AssertVerificationData("haris@example.com", before);
        Assert.AreEqual("A new code has been sent to your email.", _controller.TempData.Peek("ResentMessage"));
        _email.Verify(e => e.SendEmailAsync("haris@example.com", "New Verification Code",
            $"<p>Your new verification code is: <strong>{code}</strong></p>"), Times.Once);
        _email.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ResendCode_EmailServiceThrows_PropagatesExceptionWithoutSuccessMessage()
    {
        // Arrange
        SetVerificationData();
        var error = new InvalidOperationException("SMTP unavailable");
        _email.Setup(e => e.SendEmailAsync("haris@example.com", "New Verification Code", It.IsAny<string>()))
            .ThrowsAsync(error);

        // Act
        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _controller.ResendCode());

        // Assert
        Assert.AreSame(error, actual);
        Assert.IsNull(_controller.TempData.Peek("ResentMessage"));
        _email.Verify(e => e.SendEmailAsync("haris@example.com", "New Verification Code", It.IsAny<string>()), Times.Once);
    }

    private void SetVerificationData(DateTime? generatedAt = null)
    {
        _controller.TempData["VerificationCode"] = "123456";
        _controller.TempData["UserEmail"] = "haris@example.com";
        _controller.TempData["CodeGeneratedAt"] = generatedAt ?? DateTime.UtcNow;
    }
}
