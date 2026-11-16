using Aplikacija.Models;
using Aplikacija.Tests.Helpers;

namespace Aplikacija.Tests.Models;

[TestClass, TestCategory("Model")]
public class NarudzbaTestovi
{
    [TestMethod]
    public void Konstruktor_NovaNarudzba_PostavljaPodrazumijevaneVrijednosti()
    {
        var prije = DateTime.Now;

        var narudzba = new Narudzba();

        var poslije = DateTime.Now;
        Assert.AreEqual(0, narudzba.Id);
        Assert.AreEqual(TipNarudzbe.Narudzba, narudzba.Tip);
        Assert.IsNull(narudzba.Proizvod);
        Assert.IsNull(narudzba.Poruka);
        Assert.IsNull(narudzba.KorisnikId);
        Assert.IsNull(narudzba.Korisnik);
        Assert.AreEqual(0, narudzba.SesijaId);
        Assert.IsNull(narudzba.Sesija);
        Assert.IsTrue(narudzba.VrijemeSlanja >= prije && narudzba.VrijemeSlanja <= poslije,
            "VrijemeSlanja treba biti trenutak kreiranja objekta.");
    }

    [TestMethod]
    public void Svojstva_PostavljeneVrijednostiIVeze_CuvaVrijednostiIReference()
    {
        var korisnik = new Korisnik { Id = "k1", Ime = "Ana", Prezime = "Anic" };
        var sesija = new Sesija { Id = 3 };
        var vrijeme = new DateTime(2026, 11, 15, 20, 0, 0);

        var narudzba = new Narudzba
        {
            Id = 9, Tip = TipNarudzbe.Poruka, Proizvod = "Coca-Cola", Poruka = "Treba pomoc",
            VrijemeSlanja = vrijeme, KorisnikId = "k1", Korisnik = korisnik, SesijaId = 3, Sesija = sesija
        };

        Assert.AreEqual(9, narudzba.Id);
        Assert.AreEqual(TipNarudzbe.Poruka, narudzba.Tip);
        Assert.AreEqual("Coca-Cola", narudzba.Proizvod);
        Assert.AreEqual("Treba pomoc", narudzba.Poruka);
        Assert.AreEqual(vrijeme, narudzba.VrijemeSlanja);
        Assert.AreEqual("k1", narudzba.KorisnikId);
        Assert.AreSame(korisnik, narudzba.Korisnik);
        Assert.AreEqual(3, narudzba.SesijaId);
        Assert.AreSame(sesija, narudzba.Sesija);
    }

    [TestMethod, TestCategory("JSON")]
    [DynamicData(nameof(PlacanjeNarudzbaPodaci.ValidacijaNarudzbeJson), typeof(PlacanjeNarudzbaPodaci), DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(PlacanjeNarudzbaPodaci.NazivSlucaja), DynamicDataDisplayNameDeclaringType = typeof(PlacanjeNarudzbaPodaci))]
    public void Validacija_DuzinaTekstaIzJson_VracaOcekivaniBrojGresaka(string opis, string svojstvo, int duzina, int ocekivaniBrojGresaka)
    {
        var narudzba = new Narudzba { KorisnikId = "k1", SesijaId = 1 };
        typeof(Narudzba).GetProperty(svojstvo)!.SetValue(narudzba, PlacanjeNarudzbaPodaci.TekstDuzine(duzina));

        var greske = Validacija.ProvjeriSvojstvo(narudzba, svojstvo);

        Assert.AreEqual(ocekivaniBrojGresaka, greske.Count, opis);
    }

    [TestMethod]
    [DataRow(nameof(Narudzba.Proizvod), "Order must be between 2 and 100 characters long.")]
    [DataRow(nameof(Narudzba.Poruka), "Message must be between 2 and 300 characters long.")]
    public void Validacija_PrekratakTekst_VracaPorukuGreske(string svojstvo, string ocekivanaPoruka)
    {
        var narudzba = new Narudzba();
        typeof(Narudzba).GetProperty(svojstvo)!.SetValue(narudzba, "a");

        var greske = Validacija.ProvjeriSvojstvo(narudzba, svojstvo);

        Assert.AreEqual(ocekivanaPoruka, greske.Single().ErrorMessage);
    }

    [TestMethod]
    public void Validacija_IspravnaNarudzba_NemaGresaka()
    {
        var narudzba = new Narudzba { Tip = TipNarudzbe.Narudzba, Proizvod = "Sok", KorisnikId = "k1", SesijaId = 1 };

        bool ispravno = ValidationHelper.IsValid(narudzba);

        Assert.IsTrue(ispravno);
    }
}
