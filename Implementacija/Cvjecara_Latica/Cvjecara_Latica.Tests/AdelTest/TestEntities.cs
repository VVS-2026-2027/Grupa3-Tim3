using Cvjecara_Latica.Models;

namespace Cvjecara_Latica.Tests.AdelTest;

// Fabrike validnih objekata, sva polja popunjena (da ni jedan "required" ne smeta).
public static class TestEntities
{
    public static Discount NewDiscount(int id = 1, string code = "SUMMER-10") => new()
    {
        DiscountID = id,
        DiscountCode = code,
        DiscountAmount = 10,
        DiscountType = DiscountType.PercentageOff,
        DiscountBegins = new DateTime(2026, 1, 1),
        PersonID = "person-1"
    };

    public static Payment NewPayment(int id = 1, int? discountId = null) => new()
    {
        PaymentID = id,
        PaymentType = PaymentType.Cash,
        DiscountID = discountId,
        PayedAmount = 50,
        DeliveryAddress = "Main Street 5, Sarajevo",
        BankAccount = null
    };

    public static Product NewProduct(int id = 1) => new()
    {
        ProductID = id,
        Name = "Red Rose",
        ImageUrl = "/img/rose.png",
        Price = 5,
        FlowerType = "Rose",
        Stock = 10,
        Category = "Flowers",
        Description = "A bouquet of red roses.",
        productType = "Bouquet",
        Color = "Red",
        IsBestSeller = false
    };

    // PRETPOSTAVKA: ProductSale ima ProductSalesID, ProductID i SalesDate (DateTime).
    public static ProductSale NewProductSale(int id = 1, int productId = 1) => new()
    {
        ProductSalesID = id,
        ProductID = productId,
        SalesDate = new DateTime(2026, 1, 1)
    };
}
