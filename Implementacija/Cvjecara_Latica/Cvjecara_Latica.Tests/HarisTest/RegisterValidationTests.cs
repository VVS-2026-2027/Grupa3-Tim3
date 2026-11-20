using System.ComponentModel.DataAnnotations;
using Cvjecara_Latica.Models;

namespace Cvjecara_Latica.Tests.HarisTest;

[TestClass]
[TestCategory("Haris")]
public class RegisterValidationTests
{
    private static IEnumerable<object?[]> CasesFromCsv(bool expectedValid) =>
        CsvTestData.Read("register_validation.csv")
            .Where(row => bool.Parse(row[3]) == expectedValid)
            .Select(row => new object?[]
            {
                row[0], row[1], row[2] == "<NULL>" ? null : row[2],
                row[1] is nameof(Register.Password) or nameof(Register.ConfirmPassword) ? (row[2] == "<NULL>" ? null : row[2]) : HomeControllerTests.ValidRegistration().ConfirmPassword,
                row[4]
            });

    public static IEnumerable<object?[]> ValidCasesFromCsv() => CasesFromCsv(true);

    public static IEnumerable<object?[]> InvalidCasesFromCsv() => CasesFromCsv(false);

    [TestMethod]
    [DynamicData(nameof(ValidCasesFromCsv))]
    public void Validate_ValidCsvFieldValue_HasNoErrors(
        string caseName, string propertyName, string? value, string? confirmPassword, string expectedError)
    {
        // Arrange
        var model = HomeControllerTests.ValidRegistration();
        typeof(Register).GetProperty(propertyName)!.SetValue(model, value);
        model.ConfirmPassword = confirmPassword!;
        var errors = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(model, new ValidationContext(model), errors, true);

        // Assert
        Assert.IsTrue(valid, caseName);
        Assert.IsEmpty(errors, caseName);
    }

    [TestMethod]
    [DynamicData(nameof(InvalidCasesFromCsv))]
    public void Validate_InvalidCsvFieldValue_ReturnsExpectedError(
        string caseName, string propertyName, string? value, string? confirmPassword, string expectedError)
    {
        // Arrange
        var model = HomeControllerTests.ValidRegistration();
        typeof(Register).GetProperty(propertyName)!.SetValue(model, value);
        model.ConfirmPassword = confirmPassword!;
        var errors = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(model, new ValidationContext(model), errors, true);

        // Assert
        Assert.IsFalse(valid, caseName);
        Assert.IsTrue(errors.Any(e => e.ErrorMessage == expectedError && e.MemberNames.Contains(propertyName)), caseName);
    }

    [TestMethod]
    public void Validate_ValidRegistration_PreservesAllPropertyValues()
    {
        // Arrange
        var model = HomeControllerTests.ValidRegistration();
        var errors = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(model, new ValidationContext(model), errors, true);

        // Assert
        Assert.IsTrue(valid);
        Assert.IsEmpty(errors);
        Assert.AreEqual("Haris", model.FirstName);
        Assert.AreEqual("Test", model.LastName);
        Assert.AreEqual("Test ulica 1", model.HomeAdress);
        Assert.AreEqual("haris@example.com", model.Email);
        Assert.AreEqual("+38761123456", model.PhoneNumber);
        Assert.AreEqual("Password1!", model.Password);
        Assert.AreEqual(model.Password, model.ConfirmPassword);
    }
}
