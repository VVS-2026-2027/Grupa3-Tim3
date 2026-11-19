using System.ComponentModel.DataAnnotations;
using Cvjecara_Latica.Models;

namespace Cvjecara_Latica.Tests.HarisTest;

[TestClass]
[TestCategory("Haris")]
public class RegisterValidationTests
{
    public static IEnumerable<object?[]> ValidationCasesFromCsv()
    {
        foreach (var row in CsvTestData.Read("register_validation.csv"))
            yield return new object?[] { row[0], row[1], row[2] == "<NULL>" ? null : row[2], bool.Parse(row[3]), row[4] };
    }

    [TestMethod]
    [DynamicData(nameof(ValidationCasesFromCsv))]
    public void Validate_CsvFieldValue_ReturnsExpectedValidationResult(
        string caseName, string propertyName, string? value, bool expectedValid, string expectedError)
    {
        // Arrange: mijenja se samo jedno polje ispravnog modela, prema CSV ulazu.
        var model = HomeControllerTests.ValidRegistration();
        typeof(Register).GetProperty(propertyName)!.SetValue(model, value);
        if (propertyName == nameof(Register.Password)) model.ConfirmPassword = value!;
        var errors = new List<ValidationResult>();

        // Act: testira se stvarna DataAnnotations validacija svih svojstava modela.
        var valid = Validator.TryValidateObject(model, new ValidationContext(model), errors, true);

        // Assert: provjeravaju se validnost, konkretna poruka i pripadnost greške polju.
        Assert.AreEqual(expectedValid, valid, caseName);
        if (expectedValid)
        {
            Assert.IsEmpty(errors, caseName);
        }
        else
        {
            Assert.IsTrue(errors.Any(e => e.ErrorMessage == expectedError && e.MemberNames.Contains(propertyName)),
                $"{caseName}: {string.Join("; ", errors.Select(e => e.ErrorMessage))}");
        }
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
