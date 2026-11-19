using Aplikacija.Models;
using Aplikacija.Tests.Helpers;

namespace Aplikacija.Tests.Models;

[TestClass, TestCategory("Model")]
public class PlacanjeTestovi
{
    private static Placanje IspravnoPlacanje(double iznos = 15.5) => new()
    {
        Iznos = iznos,
        KorisnikId = "k1",
        RezervacijaId = 1,
        MetodaPlacanja = "Cash",
        DatumPlacanja = new DateTime(2026, 11, 15, 12, 0, 0)
    };

    [TestMethod]
    public void Konstruktor_NovoPlacanje_PostavljaPodrazumijevaneVrijednosti()
    {
        var placanje = new Placanje();

        Assert.AreEqual(0, placanje.Id);
        Assert.AreEqual(0.0, placanje.Iznos);
        Assert.IsNull(placanje.KorisnikId);
        Assert.IsNull(placanje.Korisnik);
        Assert.IsNull(placanje.SesijaId);
        Assert.IsNull(placanje.Sesija);
        Assert.AreEqual(0, placanje.RezervacijaId);
        Assert.IsNull(placanje.Rezervacija);
        Assert.IsNull(placanje.MetodaPlacanja);
        Assert.AreEqual(default, placanje.DatumPlacanja);
    }

    [TestMethod]
    public void Svojstva_PostavljeneVrijednostiIVeze_CuvaVrijednostiIReference()
    {
        var korisnik = new Korisnik { Id = "k1", Ime = "Ana", Prezime = "Anic" };
        var sesija = new Sesija { Id = 3 };
        var rezervacija = new Rezervacija { Id = 4 };
        var datum = new DateTime(2026, 11, 15, 18, 30, 0);

        var placanje = new Placanje
        {
            Id = 7, Iznos = 12.75, KorisnikId = "k1", Korisnik = korisnik,
            SesijaId = 3, Sesija = sesija, RezervacijaId = 4, Rezervacija = rezervacija,
            MetodaPlacanja = "Card", DatumPlacanja = datum
        };

        Assert.AreEqual(7, placanje.Id);
        Assert.AreEqual(12.75, placanje.Iznos, 0.0001);
        Assert.AreEqual("k1", placanje.KorisnikId);
        Assert.AreSame(korisnik, placanje.Korisnik);
        Assert.AreEqual(3, placanje.SesijaId);
        Assert.AreSame(sesija, placanje.Sesija);
        Assert.AreEqual(4, placanje.RezervacijaId);
        Assert.AreSame(rezervacija, placanje.Rezervacija);
        Assert.AreEqual("Card", placanje.MetodaPlacanja);
        Assert.AreEqual(datum, placanje.DatumPlacanja);
    }

    [TestMethod]
    public void Sesija_UklanjanjeOpcionalneVeze_DozvoljavaNull()
    {
        var placanje = new Placanje { SesijaId = 3, Sesija = new Sesija { Id = 3 } };

        placanje.SesijaId = null;
        placanje.Sesija = null;

        Assert.IsNull(placanje.SesijaId);
        Assert.IsNull(placanje.Sesija);
    }

    [TestMethod, TestCategory("CSV")]
    [DynamicData(nameof(PlacanjeNarudzbaPodaci.IznosiPlacanjaCsv), typeof(PlacanjeNarudzbaPodaci), DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(PlacanjeNarudzbaPodaci.NazivSlucaja), DynamicDataDisplayNameDeclaringType = typeof(PlacanjeNarudzbaPodaci))]
    public void Iznos_VrijednostIzCsv_VracaOcekivaniBrojGresaka(string slucaj, double iznos, int ocekivaniBrojGresaka)
    {
        var placanje = IspravnoPlacanje(iznos);

        var greske = Validacija.ProvjeriSvojstvo(placanje, nameof(Placanje.Iznos));

        Assert.AreEqual(ocekivaniBrojGresaka, greske.Count, slucaj);
    }

    [TestMethod]
    public void Iznos_VrijednostVanOpsega_VracaPorukuGreske()
    {
        var placanje = IspravnoPlacanje(0);

        var greske = Validacija.ProvjeriSvojstvo(placanje, nameof(Placanje.Iznos));

        Assert.AreEqual("Amount must be greater than 0!", greske.Single().ErrorMessage);
    }

    [TestMethod]
    [DataRow(null, 1)]
    [DataRow("", 1)]
    [DataRow("   ", 1)]
    [DataRow("Cash", 0)]
    [DataRow("Card", 0)]
    [DataRow("Loyalty", 0)]
    public void MetodaPlacanja_RazliciteVrijednosti_ObaveznoPolje(string? metoda, int ocekivaniBrojGresaka)
    {
        var placanje = IspravnoPlacanje();
        placanje.MetodaPlacanja = metoda!;

        var greske = Validacija.ProvjeriSvojstvo(placanje, nameof(Placanje.MetodaPlacanja));

        Assert.AreEqual(ocekivaniBrojGresaka, greske.Count);
    }

    [TestMethod]
    public void Validacija_IspravnoPlacanje_NemaGresaka()
    {
        var placanje = IspravnoPlacanje();

        bool ispravno = ValidationHelper.IsValid(placanje);

        Assert.IsTrue(ispravno);
    }

    [TestMethod]
    public void Validacija_NeispravanIznosIMetoda_PrijavljujeObjeGreske()
    {
        var placanje = IspravnoPlacanje(-1);
        placanje.MetodaPlacanja = null!;

        var greske = ValidationHelper.Validate(placanje);

        CollectionAssert.AreEquivalent(
            new[] { nameof(Placanje.Iznos), nameof(Placanje.MetodaPlacanja) },
            greske.SelectMany(g => g.MemberNames).ToArray());
    }
}
