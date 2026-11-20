using System.ComponentModel.DataAnnotations;
using Cvjecara_Latica.Models;

namespace Cvjecara_Latica.Tests.AdelTest;

[TestClass]
public class PaymentValidationTests
{
    public static IEnumerable<object?[]> PaymentCases() => TestDataLoader.PaymentCases();

    private static List<ValidationResult> Validate(Payment payment)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(payment, new ValidationContext(payment), results, validateAllProperties: true);
        return results;
    }

    [TestMethod]
    [DynamicData(nameof(PaymentCases))]
    public void Payment_Validation_MatchesExpectedResult_FromXml(
        double amount, string? address, string? bankAccount, bool expectedValid)
    {
        var payment = TestEntities.NewPayment();
        payment.PayedAmount = amount;
        payment.DeliveryAddress = address!;
        payment.BankAccount = bankAccount;

        var errors = Validate(payment);

        Assert.AreEqual(expectedValid, errors.Count == 0);
    }

    [TestMethod]
    public void PaymentType_UndefinedEnumValue_ReportsErrorOnPaymentTypeMember()
    {
        var payment = TestEntities.NewPayment();
        payment.PaymentType = (PaymentType)99;

        var errors = Validate(payment);

        Assert.HasCount(1, errors);
        var error = errors.Single();
        CollectionAssert.Contains(error.MemberNames.ToList(), "PaymentType");
    }

    [TestMethod]
    public void Payment_AllFieldsValid_HasNoErrors()
    {
        var payment = TestEntities.NewPayment();

        var errors = Validate(payment);

        Assert.IsEmpty(errors);
    }
}
