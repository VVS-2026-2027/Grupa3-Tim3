using System.Text.Json;
using System.Xml.Linq;

namespace Cvjecara_Latica.Tests.AdelTest;

// Učitava pohranjene testne podatke (JSON i XML) za data driven testove.
public static class TestDataLoader
{
    private record DiscountCodeCase(string? Code, bool IsValid);

    // Traži fajl u izlaznom folderu, a ako ga tamo nema, penje se kroz roditeljske foldere
    // (korijen projekta) i provjerava TestData/Adel (dogovoreni folder tima), TestData i AdelTest/TestData.
    private static string PathFor(string fileName)
    {
        var tried = new List<string>();
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            foreach (var relative in new[] { Path.Combine("TestData", "Adel"), "TestData", Path.Combine("AdelTest", "TestData") })
            {
                var candidate = Path.Combine(dir.FullName, relative, fileName);
                if (File.Exists(candidate)) return candidate;
                tried.Add(candidate);
            }
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Testni podaci nisu pronađeni. Pokušano: " + string.Join(" | ", tried.Take(6)), fileName);
    }

    // JSON: [{ "code": "...", "isValid": true }, ...]
    public static IEnumerable<object?[]> DiscountCodeCases()
    {
        using var stream = File.OpenRead(PathFor("discount_codes.json"));
        var cases = JsonSerializer.Deserialize<List<DiscountCodeCase>>(
            stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return cases.Select(c => new object?[] { c.Code, c.IsValid });
    }

    // XML: <Case><PayedAmount/><DeliveryAddress/><BankAccount/><IsValid/></Case>
    // Element koji nedostaje = null vrijednost.
    public static IEnumerable<object?[]> PaymentCases()
    {
        var document = XDocument.Load(PathFor("payments.xml"));
        return document.Root!.Elements("Case").Select(c => new object?[]
        {
            (double)c.Element("PayedAmount")!,
            (string?)c.Element("DeliveryAddress"),
            (string?)c.Element("BankAccount"),
            (bool)c.Element("IsValid")!
        });
    }
}