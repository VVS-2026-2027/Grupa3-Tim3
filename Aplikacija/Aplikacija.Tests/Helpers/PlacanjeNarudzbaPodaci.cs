using System.Globalization;

namespace Aplikacija.Tests.Helpers;

public record NarudzbaValidacijaPodaci(string Opis, string Svojstvo, int Duzina, int BrojGresaka);

// Izvori podataka za data driven testove klasa Placanje, Narudzba i TipNarudzbe.
public static class PlacanjeNarudzbaPodaci
{
    // Forma 1: CSV (slucaj, iznos, ocekivani broj gresaka)
    public static IEnumerable<object[]> IznosiPlacanjaCsv() =>
        TestFileReader.ReadCsv("placanje_iznosi.csv")
            .Select(r => new object[] { r[0], double.Parse(r[1], CultureInfo.InvariantCulture), int.Parse(r[2]) });

    // Forma 2: JSON (duzina -1 znaci da vrijednost nije unesena)
    public static IEnumerable<object[]> ValidacijaNarudzbeJson() =>
        TestFileReader.ReadJson<NarudzbaValidacijaPodaci>("narudzba_validacija.json")
            .Select(p => new object[] { p.Opis, p.Svojstvo, p.Duzina, p.BrojGresaka });

    // Forma 3: XML (brojcana vrijednost enuma, ocekivani naziv, da li je vrijednost definisana)
    public static IEnumerable<object[]> TipoviNarudzbeXml() =>
        TestFileReader.ReadXml("tipovi_narudzbe.xml")
            .Select(e => new object[]
            {
                int.Parse((string)e.Attribute("vrijednost")!, CultureInfo.InvariantCulture),
                (string)e.Attribute("naziv")!,
                bool.Parse((string)e.Attribute("definisan")!)
            });

    public static string? TekstDuzine(int duzina) => duzina < 0 ? null : new string('a', duzina);

    public static string NazivSlucaja(System.Reflection.MethodInfo metoda, object[] podaci) =>
        $"{metoda.Name} ({podaci[0]})";
}
