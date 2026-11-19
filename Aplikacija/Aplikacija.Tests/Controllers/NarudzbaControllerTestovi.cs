using System.Reflection;
using Aplikacija.Controllers;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Aplikacija.Tests.Controllers;

[TestClass, TestCategory("Kontroler")]
public class NarudzbaControllerTestovi
{
    private const string GreskaSesija = "You can send orders or messages only during an active session.";
    private const string GreskaProizvod = "Enter what you want to order.";
    private const string GreskaPoruka = "Enter a message.";
    private const string Uspjeh = "Your request has been sent.";

    private string _baza = null!;

    [TestInitialize]
    public void Postavi()
    {
        // Svaki test dobija svoju praznu in-memory bazu (zamjena za pravu bazu).
        _baza = TestDb.NewName();
    }

    // ---------------- pomocne metode ----------------

    private ApplicationDbContext NoviKontekst() => TestDb.Create(_baza);

    // Kontroler sa prijavljenim korisnikom i zamjenskim TempData spremnikom.
    private NarudzbaController NoviKontroler(string korisnikId = "k1", ApplicationDbContext? kontekst = null) =>
        new NarudzbaController(kontekst ?? NoviKontekst()).WithUser(korisnikId, "User");

    private void DodajKorisnikaIUredjaj(params string[] korisnici)
    {
        using var kontekst = NoviKontekst();
        foreach (string id in korisnici.DefaultIfEmpty("k1"))
            kontekst.SeedKorisnik(id);
        kontekst.SeedDevice("PC", 10, 5);
    }

    private void DodajSesiju(int id, string? korisnikId = "k1", StatusSesije status = StatusSesije.Aktivna,
        double pocetakPrijeSati = 1, double krajZaSati = 1)
    {
        using var kontekst = NoviKontekst();
        kontekst.Sesija.Add(new Sesija
        {
            Id = id, UredjajId = 10, KorisnikId = korisnikId, Status = status,
            VrijemePocetka = DateTime.Now.AddHours(-pocetakPrijeSati),
            VrijemeZavrsetka = DateTime.Now.AddHours(krajZaSati)
        });
        kontekst.SaveChanges();
    }

    private List<Narudzba> SveNarudzbe()
    {
        using var kontekst = NoviKontekst();
        return kontekst.Narudzba.AsNoTracking().ToList();
    }

    private static void ProvjeriPreusmjerenjeNaPocetnu(IActionResult rezultat)
    {
        var preusmjerenje = rezultat.Is<RedirectToActionResult>();
        Assert.AreEqual("Index", preusmjerenje.ActionName);
        Assert.AreEqual("Home", preusmjerenje.ControllerName);
    }

    private static void ProvjeriGresku(NarudzbaController kontroler, string ocekivanaGreska)
    {
        Assert.AreEqual(ocekivanaGreska, kontroler.TempData["Greska"]);
        Assert.IsNull(kontroler.TempData["Poruka"]);
    }

    // ---------------- Autorizacija ----------------

    [TestMethod, TestCategory("Autorizacija")]
    public void Kontroler_Atributi_ZahtijevaPrijavu()
    {
        var atribut = typeof(NarudzbaController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.IsNotNull(atribut);
        Assert.IsNull(atribut.Roles);
    }

    [TestMethod, TestCategory("Autorizacija")]
    [DataRow(nameof(NarudzbaController.Index), "Administrator,Employee")]
    [DataRow(nameof(NarudzbaController.QuickSend), "User")]
    public void Akcija_Atributi_DozvoljavaOcekivaneUloge(string akcija, string ocekivaneUloge)
    {
        var metoda = typeof(NarudzbaController).GetMethod(akcija)!;

        var atribut = metoda.GetCustomAttribute<AuthorizeAttribute>();

        Assert.AreEqual(ocekivaneUloge, atribut?.Roles);
    }

    [TestMethod, TestCategory("Autorizacija")]
    public void QuickSend_Atributi_SamoPostSaAntiForgeryTokenom()
    {
        var metoda = typeof(NarudzbaController).GetMethod(nameof(NarudzbaController.QuickSend))!;

        Assert.IsNotNull(metoda.GetCustomAttribute<HttpPostAttribute>());
        Assert.IsNotNull(metoda.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    // ---------------- Index ----------------

    [TestMethod]
    public async Task Index_PraznaBaza_VracaPraznuListu()
    {
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Index();

        Assert.AreEqual(0, rezultat.ModelOf<List<Narudzba>>().Count);
    }

    [TestMethod]
    public async Task Index_ViseNarudzbi_SortiraOdNajnovijeIUcitavaVeze()
    {
        DodajKorisnikaIUredjaj();
        DodajSesiju(1);
        var sada = DateTime.Now;
        using (var kontekst = NoviKontekst())
        {
            kontekst.Narudzba.AddRange(
                new Narudzba { Id = 1, Tip = TipNarudzbe.Narudzba, Proizvod = "Sok", KorisnikId = "k1", SesijaId = 1, VrijemeSlanja = sada.AddMinutes(-30) },
                new Narudzba { Id = 2, Tip = TipNarudzbe.Poruka, Poruka = "Pomoc", KorisnikId = "k1", SesijaId = 1, VrijemeSlanja = sada },
                new Narudzba { Id = 3, Tip = TipNarudzbe.Narudzba, Proizvod = "Cips", KorisnikId = "k1", SesijaId = 1, VrijemeSlanja = sada.AddMinutes(-10) });
            kontekst.SaveChanges();
        }
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Index();

        var model = rezultat.ModelOf<List<Narudzba>>();
        CollectionAssert.AreEqual(new[] { 2, 3, 1 }, model.Select(n => n.Id).ToArray());
        Assert.IsTrue(model.All(n => n.Korisnik.Id == "k1"));
        Assert.IsTrue(model.All(n => n.Sesija.Uredjaj is PC { Id: 10 }));
    }

    // ---------------- QuickSend: provjera aktivne sesije ----------------

    [TestMethod]
    public async Task QuickSend_KorisnikBezSesije_PostavljaGreskuINeSnima()
    {
        DodajKorisnikaIUredjaj();
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.QuickSend(TipNarudzbe.Narudzba, "Sok", null);

        ProvjeriPreusmjerenjeNaPocetnu(rezultat);
        ProvjeriGresku(kontroler, GreskaSesija);
        Assert.AreEqual(0, SveNarudzbe().Count);
    }

    [TestMethod]
    public async Task QuickSend_AktivnaSesijaDrugogKorisnika_PostavljaGreskuINeSnima()
    {
        DodajKorisnikaIUredjaj("k1", "k2");
        DodajSesiju(1, korisnikId: "k2");
        var kontroler = NoviKontroler("k1");

        await kontroler.QuickSend(TipNarudzbe.Narudzba, "Sok", null);

        ProvjeriGresku(kontroler, GreskaSesija);
        Assert.AreEqual(0, SveNarudzbe().Count);
    }

    [TestMethod]
    [DataRow(StatusSesije.Zavrsena)]
    [DataRow(StatusSesije.Pauzirana)]
    [DataRow(StatusSesije.Istekla)]
    public async Task QuickSend_SesijaNijeAktivna_PostavljaGreskuINeSnima(StatusSesije status)
    {
        DodajKorisnikaIUredjaj();
        DodajSesiju(1, status: status);
        var kontroler = NoviKontroler();

        await kontroler.QuickSend(TipNarudzbe.Narudzba, "Sok", null);

        ProvjeriGresku(kontroler, GreskaSesija);
        Assert.AreEqual(0, SveNarudzbe().Count);
    }

    [TestMethod]
    [DataRow(-1.0, 2.0, DisplayName = "Sesija jos nije pocela")]
    [DataRow(3.0, -1.0, DisplayName = "Sesija je vec zavrsila")]
    public async Task QuickSend_AktivnaSesijaVanVremenskogIntervala_PostavljaGreskuINeSnima(double pocetakPrijeSati, double krajZaSati)
    {
        DodajKorisnikaIUredjaj();
        DodajSesiju(1, pocetakPrijeSati: pocetakPrijeSati, krajZaSati: krajZaSati);
        var kontroler = NoviKontroler();

        await kontroler.QuickSend(TipNarudzbe.Narudzba, "Sok", null);

        ProvjeriGresku(kontroler, GreskaSesija);
        Assert.AreEqual(0, SveNarudzbe().Count);
    }

    // ---------------- QuickSend: provjera unosa ----------------

    [TestMethod]
    [DataRow(null), DataRow(""), DataRow("   "), DataRow("\t\n")]
    public async Task QuickSend_NarudzbaBezProizvoda_PostavljaGreskuINeSnima(string? proizvod)
    {
        DodajKorisnikaIUredjaj();
        DodajSesiju(1);
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.QuickSend(TipNarudzbe.Narudzba, proizvod, "poruka se ignorise");

        ProvjeriPreusmjerenjeNaPocetnu(rezultat);
        ProvjeriGresku(kontroler, GreskaProizvod);
        Assert.AreEqual(0, SveNarudzbe().Count);
    }

    [TestMethod]
    [DataRow(null), DataRow(""), DataRow("   ")]
    public async Task QuickSend_PorukaBezTeksta_PostavljaGreskuINeSnima(string? poruka)
    {
        DodajKorisnikaIUredjaj();
        DodajSesiju(1);
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.QuickSend(TipNarudzbe.Poruka, "proizvod se ignorise", poruka);

        ProvjeriPreusmjerenjeNaPocetnu(rezultat);
        ProvjeriGresku(kontroler, GreskaPoruka);
        Assert.AreEqual(0, SveNarudzbe().Count);
    }

    // ---------------- QuickSend: uspjesno slanje ----------------

    [TestMethod]
    public async Task QuickSend_IspravnaNarudzba_SnimaSamoProizvodIVezujeZaSesiju()
    {
        DodajKorisnikaIUredjaj();
        DodajSesiju(7);
        var kontroler = NoviKontroler();
        var prije = DateTime.Now;

        var rezultat = await kontroler.QuickSend(TipNarudzbe.Narudzba, "Coca-Cola", "ne treba se snimiti");

        ProvjeriPreusmjerenjeNaPocetnu(rezultat);
        Assert.AreEqual(Uspjeh, kontroler.TempData["Poruka"]);
        Assert.IsNull(kontroler.TempData["Greska"]);
        var narudzba = SveNarudzbe().Single();
        Assert.AreEqual(TipNarudzbe.Narudzba, narudzba.Tip);
        Assert.AreEqual("Coca-Cola", narudzba.Proizvod);
        Assert.IsNull(narudzba.Poruka);
        Assert.AreEqual("k1", narudzba.KorisnikId);
        Assert.AreEqual(7, narudzba.SesijaId);
        Assert.IsTrue(narudzba.VrijemeSlanja >= prije && narudzba.VrijemeSlanja <= DateTime.Now);
    }

    [TestMethod]
    public async Task QuickSend_IspravnaPoruka_SnimaSamoPoruku()
    {
        DodajKorisnikaIUredjaj();
        DodajSesiju(7);
        var kontroler = NoviKontroler();

        await kontroler.QuickSend(TipNarudzbe.Poruka, "ne treba se snimiti", "Upalite klimu");

        Assert.AreEqual(Uspjeh, kontroler.TempData["Poruka"]);
        var narudzba = SveNarudzbe().Single();
        Assert.AreEqual(TipNarudzbe.Poruka, narudzba.Tip);
        Assert.AreEqual("Upalite klimu", narudzba.Poruka);
        Assert.IsNull(narudzba.Proizvod);
    }

    [TestMethod]
    public async Task QuickSend_ViseSesijaKorisnika_VezujeNarudzbuZaAktivnuSesiju()
    {
        DodajKorisnikaIUredjaj();
        DodajSesiju(1, status: StatusSesije.Zavrsena, pocetakPrijeSati: 5, krajZaSati: -3);
        DodajSesiju(2);
        var kontroler = NoviKontroler();

        await kontroler.QuickSend(TipNarudzbe.Narudzba, "Voda", null);

        Assert.AreEqual(2, SveNarudzbe().Single().SesijaId);
    }

    // ---------------- QuickSend: izuzeci ----------------

    [TestMethod, TestCategory("ZamjenskiObjekat")]
    public async Task QuickSend_GreskaBazePriSnimanju_PropagiraIzuzetakBezPorukeUspjeha()
    {
        DodajKorisnikaIUredjaj();
        DodajSesiju(1);
        var zamjenski = new Mock<ApplicationDbContext>(TestDb.Options(_baza)) { CallBase = true };
        zamjenski.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Baza nije dostupna"));
        var kontroler = NoviKontroler(kontekst: zamjenski.Object);

        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => kontroler.QuickSend(TipNarudzbe.Narudzba, "Sok", null));

        zamjenski.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsNull(kontroler.TempData["Poruka"]);
        Assert.AreEqual(0, SveNarudzbe().Count);
    }
}
