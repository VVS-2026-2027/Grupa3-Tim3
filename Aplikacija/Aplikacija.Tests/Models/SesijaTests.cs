using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;

namespace Aplikacija.Tests.Models;

[TestClass]
[TestCategory("Model")]
public class SesijaTests
{
    private Sesija _sesija = null!;

    [TestInitialize]
    public void Setup()
    {
        _sesija = new Sesija
        {
            Id = 7,
            VrijemePocetka = new DateTime(2026, 11, 15, 10, 0, 0),
            Status = StatusSesije.Pauzirana
        };
    }

    [TestMethod]
    public void GetId_PostavljenId_VracaId()
    {
        Assert.AreEqual(7, _sesija.GetId());
    }

    [TestMethod]
    public void GetVrijemePocetka_PostavljenoVrijeme_VracaVrijeme()
    {
        Assert.AreEqual(new DateTime(2026, 11, 15, 10, 0, 0), _sesija.GetVrijemePocetka());
    }

    [TestMethod]
    public void GetStatus_PostavljenStatus_VracaStatus()
    {
        Assert.AreEqual(StatusSesije.Pauzirana, _sesija.GetStatus());
    }

    [TestMethod]
    public void NovaSesija_PodrazumijevaneVrijednosti_StatusJeAktivnaIGostNijeAnoniman()
    {
        var nova = new Sesija();

        Assert.AreEqual(StatusSesije.Aktivna, nova.Status);
        Assert.IsFalse(nova.AnonimniGost);
        Assert.IsNull(nova.KorisnikId);
    }

    [TestMethod]
    [DynamicData(nameof(Osoba3Data.ValidacijaSesijeJson), typeof(Osoba3Data), DynamicDataSourceType.Method)]
    public void Validacija_RazliciteGraniceVremena_VracaOcekivaniBrojGresaka(DateTime pocetak, DateTime kraj, int ocekivanoGresaka)
    {
        var sesija = new Sesija { VrijemePocetka = pocetak, VrijemeZavrsetka = kraj };
        var greske = new List<ValidationResult>();

        Validator.TryValidateObject(sesija, new ValidationContext(sesija), greske, true);

        Assert.AreEqual(ocekivanoGresaka, greske.Count);
    }

    [TestMethod]
    public void SkalarnaSvojstva_PostavljeneVrijednosti_VracajuIsteVrijednosti()
    {
        var kraj = new DateTime(2026, 11, 15, 12, 0, 0);

        var sesija = new Sesija { VrijemeZavrsetka = kraj, UredjajId = 3, KorisnikId = "k1", TakmicenjeId = 9, AnonimniGost = true };

        Assert.AreEqual(kraj, sesija.VrijemeZavrsetka);
        Assert.AreEqual(3, sesija.UredjajId);
        Assert.AreEqual("k1", sesija.KorisnikId);
        Assert.AreEqual(9, sesija.TakmicenjeId);
        Assert.IsTrue(sesija.AnonimniGost);
    }

    [TestMethod]
    public void NavigacijskaSvojstva_PostavljeneVrijednosti_VracajuIsteObjekte()
    {
        var uredjaj = new Uredjaj { Id = 3 };
        var korisnik = new Korisnik { Id = "k1" };
        var takmicenje = new Takmicenje();
        var placanja = new List<Placanje>();

        var sesija = new Sesija { Uredjaj = uredjaj, Korisnik = korisnik, Takmicenje = takmicenje, Placanja = placanja };

        Assert.AreSame(uredjaj, sesija.Uredjaj);
        Assert.AreSame(korisnik, sesija.Korisnik);
        Assert.AreSame(takmicenje, sesija.Takmicenje);
        Assert.AreSame(placanja, sesija.Placanja);
    }
}