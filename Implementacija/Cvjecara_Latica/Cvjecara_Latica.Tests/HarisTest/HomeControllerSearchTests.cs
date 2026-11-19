using System.Diagnostics;
using Cvjecara_Latica.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cvjecara_Latica.Tests.HarisTest;

public partial class HomeControllerTests
{
    [TestMethod]
    public void Index_Get_ReturnsDefaultView()
    {
        // Arrange: kontroler iz SetUp.
        // Act
        var result = _controller.Index();

        // Assert
        AssertDefaultView(result);
    }

    [TestMethod]
    public void Privacy_Get_ReturnsDefaultView()
    {
        // Arrange: kontroler iz SetUp.
        // Act
        var result = _controller.Privacy();

        // Assert
        AssertDefaultView(result);
    }

    [TestMethod]
    public void Login_Get_ReturnsDefaultView()
    {
        // Arrange: kontroler iz SetUp.
        // Act
        var result = _controller.Login();

        // Assert
        AssertDefaultView(result);
    }

    [TestMethod]
    public void Register_Get_ReturnsDefaultView()
    {
        // Arrange: kontroler iz SetUp.
        // Act
        var result = _controller.Register();

        // Assert
        AssertDefaultView(result);
    }

    [TestMethod]
    public void Contact_Get_ReturnsDefaultView()
    {
        // Arrange: kontroler iz SetUp.
        // Act
        var result = _controller.Contact();

        // Assert
        AssertDefaultView(result);
    }

    [TestMethod]
    public void Help_Get_ReturnsDefaultView()
    {
        // Arrange: kontroler iz SetUp.
        // Act
        var result = _controller.Help();

        // Assert
        AssertDefaultView(result);
    }

    [TestMethod]
    public void Search_Get_ReturnsDefaultView()
    {
        // Arrange: kontroler iz SetUp.
        // Act
        var result = _controller.Search();

        // Assert
        AssertDefaultView(result);
    }

    [TestMethod]
    public void AccessDenied_Get_ReturnsDefaultView()
    {
        // Arrange: kontroler iz SetUp.
        // Act
        var result = _controller.AccessDenied();

        // Assert
        AssertDefaultView(result);
    }

    [TestMethod]
    public void Error_ActiveActivity_UsesActivityId()
    {
        // Arrange
        using var activity = new Activity("Haris error test").Start();
        _controller.HttpContext.TraceIdentifier = "request-id";

        // Act
        var result = (ViewResult)_controller.Error();

        // Assert
        var model = (ErrorViewModel)result.Model!;
        Assert.AreEqual(activity.Id, model.RequestId);
        Assert.IsTrue(model.ShowRequestId);
    }

    [TestMethod]
    public void Error_NoActivity_UsesHttpContextTraceIdentifier()
    {
        // Arrange
        var previousActivity = Activity.Current;
        Activity.Current = null;
        _controller.HttpContext.TraceIdentifier = "haris-request-id";
        try
        {
            // Act
            var result = (ViewResult)_controller.Error();

            // Assert
            Assert.AreEqual("haris-request-id", ((ErrorViewModel)result.Model!).RequestId);
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    [TestMethod]
    [DataRow(null, null, null, null)]
    [DataRow("rose", "red", "bouquet", "10-20 USD")]
    [DataRow("invalid", "invalid", "invalid", "invalid")]
    public void Filter_AnyParameters_ReturnsFilteredResultsView(string? type, string? color, string? category, string? price)
    {
        // Arrange: Filter je trenutno samo povrat pogleda, bez upita nad bazom.
        // Act
        var result = (ViewResult)_controller.Filter(type!, color!, category!, price!);

        // Assert
        Assert.AreEqual("FilteredResults", result.ViewName);
        Assert.IsNull(result.Model);
    }

    public static IEnumerable<object[]> SearchCasesFromCsv()
    {
        foreach (var row in CsvTestData.Read("search_results.csv"))
            yield return row.Cast<object>().ToArray();
    }

    [TestMethod]
    [DynamicData(nameof(SearchCasesFromCsv))]
    public void SearchResults_CsvFilters_ReturnsExpectedProductsAndViewData(
        string caseName, string query, string flowerTypes, string colors, string category, string price, string expectedIds)
    {
        // Arrange: | razdvaja više vrijednosti unutar jedne CSV ćelije.
        SeedProducts();
        var types = string.IsNullOrEmpty(flowerTypes) ? null : flowerTypes.Split('|').ToList();
        var selectedColors = string.IsNullOrEmpty(colors) ? null : colors.Split('|').ToList();
        var expected = string.IsNullOrEmpty(expectedIds) ? Array.Empty<int>() : expectedIds.Split('|').Select(int.Parse).ToArray();

        // Act
        var result = (ViewResult)_controller.SearchResults(query, types!, selectedColors!, category, price);

        // Assert
        var products = (List<Product>)result.Model!;
        CollectionAssert.AreEquivalent(expected, products.Select(p => p.ProductID).ToArray(), caseName);
        Assert.AreEqual(query, result.ViewData["Query"]);
        Assert.AreSame(types, result.ViewData["FlowerTypes"]);
        Assert.AreSame(selectedColors, result.ViewData["Colors"]);
        Assert.AreEqual(category, result.ViewData["Category"]);
        Assert.AreEqual(price, result.ViewData["Price"]);
    }

    [TestMethod]
    public void SearchResults_NullFilters_ReturnsAllProducts()
    {
        // Arrange
        SeedProducts();

        // Act
        var result = (ViewResult)_controller.SearchResults(null!, null!, null!, null!, null!);

        // Assert
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 },
            ((List<Product>)result.Model!).Select(p => p.ProductID).ToArray());
    }

    [TestMethod]
    public void SearchResults_EmptyFilterLists_ReturnsAllProducts()
    {
        // Arrange
        SeedProducts();

        // Act
        var result = (ViewResult)_controller.SearchResults("", new List<string>(), new List<string>(), "", "");

        // Assert
        Assert.HasCount(4, (List<Product>)result.Model!);
    }

    [TestMethod]
    public void SearchResults_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange: svaki test dobija vlastitu praznu InMemory bazu.
        // Act
        var result = (ViewResult)_controller.SearchResults("", null!, null!, "", "");

        // Assert
        Assert.IsEmpty((List<Product>)result.Model!);
    }

    [TestMethod]
    public void BestSellers_MixedProducts_ReturnsOnlyBestSellers()
    {
        // Arrange
        SeedProducts();

        // Act
        var result = (ViewResult)_controller.BestSellers();

        // Assert
        CollectionAssert.AreEquivalent(new[] { 1, 3 },
            ((List<Product>)result.Model!).Select(p => p.ProductID).ToArray());
    }

    [TestMethod]
    public void BestSellers_NoBestSellers_ReturnsEmptyList()
    {
        // Arrange
        SeedProducts();
        foreach (var product in _context.Products) product.IsBestSeller = false;
        _context.SaveChanges();

        // Act
        var result = (ViewResult)_controller.BestSellers();

        // Assert
        Assert.IsEmpty((List<Product>)result.Model!);
    }

    [TestMethod]
    public void BestSellers_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange: prazna baza.
        // Act
        var result = (ViewResult)_controller.BestSellers();

        // Assert
        Assert.IsEmpty((List<Product>)result.Model!);
    }

    private static void AssertDefaultView(IActionResult result)
    {
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.IsNull(((ViewResult)result).ViewName);
        Assert.IsNull(((ViewResult)result).Model);
    }

    private void SeedProducts()
    {
        _context.Products.AddRange(
            Product(1, "Rose bouquet", "Fresh garden flowers", "Rose", "Red", "Bouquet", 10, true),
            Product(2, "Spring mix", "Sunny tulip arrangement", "Tulip", "Yellow", "Bouquet", 20, false),
            Product(3, "White gift", "Elegant arrangement", "Lily", "White", "Single", 30, true),
            Product(4, "Garden special", "Fragrant garden flowers", "Rose", "Red", "Single", 20, false));
        _context.SaveChanges();
    }

    private static Product Product(int id, string name, string description, string flowerType,
        string color, string category, double price, bool bestSeller) => new()
    {
        ProductID = id, Name = name, Description = description, FlowerType = flowerType,
        Color = color, Category = category, Price = price, IsBestSeller = bestSeller,
        ImageUrl = "test.jpg", Stock = 10, productType = "Single"
    };
}
