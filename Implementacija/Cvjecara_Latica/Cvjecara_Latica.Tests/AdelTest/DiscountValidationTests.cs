using System.ComponentModel.DataAnnotations;
using Cvjecara_Latica.Models;

namespace Cvjecara_Latica.Tests.AdelTest;

[TestClass]
public class DiscountValidationTests
{
    public static IEnumerable<object?[]> CodeCases() => TestDataLoader.DiscountCodeCases();

    private static List<ValidationResult> Validate(Discount discount)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(discount, new ValidationContext(discount), results, validateAllProperties: true);
        return results;
    }

    [TestMethod]
    [DynamicData(nameof(CodeCases))]
    public void DiscountCode_Validation_MatchesExpectedResult_FromJson(string? code, bool expectedValid)
    {
        var discount = TestEntities.NewDiscount();
        discount.DiscountCode = code!;

        var errors = Validate(discount);

        Assert.AreEqual(expectedValid, errors.Count == 0);
    }

    [TestMethod]
    public void DiscountCode_Invalid_ReportsErrorOnDiscountCodeMember()
    {
        var discount = TestEntities.NewDiscount(code: "AB");

        var errors = Validate(discount);

        Assert.HasCount(1, errors);
        var error = errors.Single();
        CollectionAssert.Contains(error.MemberNames.ToList(), "DiscountCode");
    }

    [TestMethod]
    public void NewDiscount_DefaultValues_IsNotUsed()
    {
        var discount = new Discount();

        Assert.IsFalse(discount.IsUsed);
    }
}
