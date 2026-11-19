namespace Aplikacija.Tests.Helpers;

public record SesijaValidacijaPodaci(DateTime VrijemePocetka, DateTime VrijemeZavrsetka, int BrojGresaka);

public static class Osoba3Data
{
    // Forma 1: JSON
    public static IEnumerable<object[]> ValidacijaSesijeJson() =>
        TestFileReader.ReadJson<SesijaValidacijaPodaci>("sesija_validacija.json")
            .Select(p => new object[] { p.VrijemePocetka, p.VrijemeZavrsetka, p.BrojGresaka });

    // Forma 2: CSV
    public static IEnumerable<object[]> StatusiSesijeCsv() =>
        TestFileReader.ReadCsv("statusi_sesije.csv")
            .Select(r => new object[] { int.Parse(r[0]), r[1] });
}
