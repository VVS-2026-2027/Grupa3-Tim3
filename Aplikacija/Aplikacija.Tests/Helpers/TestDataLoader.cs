using System.Globalization;
using System.Text.Json;

namespace Aplikacija.Tests.Helpers;

/// <summary>Ucitava data-driven podatke iz CSV i JSON fajlova (folder TestData).</summary>
public static class TestDataLoader
{
    private static string PathTo(string file) => Path.Combine(AppContext.BaseDirectory, "TestData", file);

    private static double D(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    private static IEnumerable<string[]> CsvRows(string file)
        => File.ReadAllLines(PathTo(file)).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Split(','));

    // placanja.csv: TipUredjaja,CijenaPoSatu,TrajanjeSati,OcekivaniIznos
    public static IEnumerable<object[]> PaymentRows()
        => CsvRows("placanjaAD.csv").Select(c => new object[] { c[0], D(c[1]), D(c[2]), D(c[3]) }).ToList();

    // loyalty.csv: UkupniBodovi,BodoviZaKoristenje,UkupnaCijena,OcekivaniIznos,OcekivaniPreostaliBodovi
    public static IEnumerable<object[]> LoyaltyRows()
        => CsvRows("loyaltyAD.csv").Select(c => new object[] { int.Parse(c[0]), int.Parse(c[1]), D(c[2]), D(c[3]), int.Parse(c[4]) }).ToList();

    // sistemi.csv: NazivSistema,ApiUrl,IntervalOsvjezavanja
    public static IEnumerable<object[]> SistemiRows()
        => CsvRows("sistemiAD.csv").Select(c => new object[] { c[0], c[1], int.Parse(c[2]) }).ToList();

    // takmicenja.json: odgovori vanjskog API-ja + ocekivano takmicenje
    public static IEnumerable<object[]> ApiCases()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(PathTo("takmicenjaAD.json")));
        return doc.RootElement.EnumerateArray()
            .Select(e => new object[]
            {
                e.GetProperty("ApiResponse").GetRawText(),
                e.GetProperty("ExpectedNaziv").GetString()!,
                e.GetProperty("ExpectedIgra").GetString()!
            }).ToList();
    }
}
