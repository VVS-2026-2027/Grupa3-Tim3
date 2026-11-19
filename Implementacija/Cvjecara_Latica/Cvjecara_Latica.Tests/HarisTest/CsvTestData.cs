using Microsoft.VisualBasic.FileIO;

namespace Cvjecara_Latica.Tests.HarisTest;

internal static class CsvTestData
{
    // Isti pristup kao kod kolega: zaglavlje se preskače, svaki CSV red je DynamicData slučaj.
    // TextFieldParser podržava i vrijednosti u navodnicima, npr. adresu koja sadrži zarez.
    public static IEnumerable<string[]> Read(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "Haris", fileName);
        using var parser = new TextFieldParser(path);
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = false;
        parser.ReadFields();
        while (!parser.EndOfData)
            yield return parser.ReadFields()!;
    }
}
