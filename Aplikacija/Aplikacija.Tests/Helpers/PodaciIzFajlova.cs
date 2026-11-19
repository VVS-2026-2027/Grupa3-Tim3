using System.Globalization;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;

namespace Aplikacija.Tests.Helpers;

// Samo infrastruktura ucitava i pretvara podatke. Testne metode nemaju petlje.
public static class PodaciIzFajlova
{
    private static string Putanja(string naziv) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", naziv);

    public static IEnumerable<object[]> OpisiKvara()
    {
        using var parser = new TextFieldParser(Putanja("OpisiKvara.csv"));
        parser.SetDelimiters(";");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = false;
        parser.ReadFields(); // Zaglavlje CSV-a.

        while (!parser.EndOfData)
        {
            string[] polja = parser.ReadFields()
                ?? throw new InvalidDataException("Nedostaje CSV red.");
            if (polja.Length != 4)
                throw new InvalidDataException("CSV red mora imati cetiri kolone.");

            yield return new object[]
            {
                polja[0], polja[1] == "<NULL>" ? null! : polja[1],
                bool.Parse(polja[2]), int.Parse(polja[3], CultureInfo.InvariantCulture)
            };
        }
    }

    public static IEnumerable<object[]> StatusiUredjaja() => Statusi("uredjaji");
    public static IEnumerable<object[]> StatusiKvara() => Statusi("kvarovi");

    private static IEnumerable<object[]> Statusi(string grupa)
    {
        using var dokument = JsonDocument.Parse(File.ReadAllText(Putanja("Statusi.json")));
        foreach (var red in dokument.RootElement.GetProperty(grupa).EnumerateArray())
        {
            yield return new object[]
            {
                red.GetProperty("slucaj").GetString()!,
                red.GetProperty("vrijednost").GetInt32(),
                red.GetProperty("ispravan").GetBoolean()
            };
        }
    }
}
