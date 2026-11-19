using Aplikacija.Models;
using Aplikacija.Tests.Helpers;

namespace Aplikacija.Tests.Models;

[TestClass]
public class TipKorisnikaTests
{
    [Owner("Ajdin.L"), TestMethod]
    public void Enum_ImaTacnoCetiriVrijednosti()
    {
        Assert.AreEqual(4, Enum.GetValues<TipKorisnika>().Length);
    }

    [Owner("Ajdin.L"), TestMethod]
    public void Enum_NaziviSuUOcekivanomRedoslijedu()
    {
        var ocekivano = new[] { "Administrator", "Zaposlenik", "RegistrovaniKorsinik", "Gost" };

        CollectionAssert.AreEqual(ocekivano, Enum.GetNames<TipKorisnika>());
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(TipKorisnika.Administrator, 0)]
    [DataRow(TipKorisnika.Zaposlenik, 1)]
    [DataRow(TipKorisnika.RegistrovaniKorsinik, 2)]
    [DataRow(TipKorisnika.Gost, 3)]
    public void Vrijednost_ImaOcekivaniCijeliBroj(TipKorisnika tip, int ocekivano)
    {
        Assert.AreEqual(ocekivano, (int)tip);
    }

    [Owner("Ajdin.L"), TestMethod]
    public void Enum_KoristiInt()
    {
        Assert.AreEqual(typeof(int), Enum.GetUnderlyingType(typeof(TipKorisnika)));
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(2, true)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(-1, false)]
    [DataRow(99, false)]
    public void IsDefined_VracaTacnoZaPostojeceVrijednosti(int vrijednost, bool ocekivano)
    {
        Assert.AreEqual(ocekivano, Enum.IsDefined(typeof(TipKorisnika), vrijednost));
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow("Administrator", TipKorisnika.Administrator)]
    [DataRow("Zaposlenik", TipKorisnika.Zaposlenik)]
    [DataRow("RegistrovaniKorsinik", TipKorisnika.RegistrovaniKorsinik)]
    [DataRow("Gost", TipKorisnika.Gost)]
    public void Parse_ValidanNaziv_VracaTip(string naziv, TipKorisnika ocekivano)
    {
        Assert.AreEqual(ocekivano, Enum.Parse<TipKorisnika>(naziv));
    }

    [Owner("Ajdin.L"), TestMethod]
    public void Parse_NepostojeciNaziv_BacaArgumentException()
    {
        TestObjekti.OcekujIzuzetak<ArgumentException>(() => Enum.Parse<TipKorisnika>("Superadmin"));
    }

    [Owner("Ajdin.L"), TestMethod]
    public void TryParse_RazlikaVelikihIMalihSlova_OvisiOIgnoreCase()
    {
        Assert.IsTrue(Enum.TryParse<TipKorisnika>("gost", ignoreCase: true, out var tip));
        Assert.AreEqual(TipKorisnika.Gost, tip);

        Assert.IsFalse(Enum.TryParse<TipKorisnika>("gost", ignoreCase: false, out _));
    }

    [Owner("Ajdin.L"), TestMethod]
    public void ToString_VracaNazivVrijednosti()
    {
        Assert.AreEqual("Zaposlenik", TipKorisnika.Zaposlenik.ToString());
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public void Default_JeAdministrator()
    {
        // Pazljivo: neinicijalizirano polje tipa TipKorisnika postaje Administrator (vrijednost 0).
        Assert.AreEqual(TipKorisnika.Administrator, default(TipKorisnika));
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public void Naziv_RegistrovaniKorsinik_SadrziSlovnuGresku()
    {
        // Dokumentuje trenutni naziv iz modela. Ako se ispravi u "RegistrovaniKorisnik",
        // ovaj test treba namjerno azurirati zajedno sa svim mjestima koja koriste enum.
        Assert.IsTrue(Enum.IsDefined(typeof(TipKorisnika), "RegistrovaniKorsinik"));
        Assert.IsFalse(Enum.IsDefined(typeof(TipKorisnika), "RegistrovaniKorisnik"));
    }
}
