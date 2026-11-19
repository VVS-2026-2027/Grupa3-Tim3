using Aplikacija.Controllers;
using Aplikacija.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Aplikacija.Tests.Controllers;

[TestClass]
[TestCategory("Controller")]
public class SesijaControllerTests
{
    private DbContextOptions<ApplicationDbContext> _options = null!;

    [TestInitialize]
    public void Setup()
    {
        // Svaki test dobija svoju praznu in-memory bazu (zamjena za pravu bazu)
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    // ---------------- pomocne metode ----------------

    private ApplicationDbContext NoviKontekst() => new ApplicationDbContext(_options);

    private SesijaController NoviKontroler() => new SesijaController(NoviKontekst());

    private static Sesija NovaSesija(int id, StatusSesije status = StatusSesije.Aktivna,
        string? korisnikId = null, DateTime? kraj = null, int uredjajId = 1) => new Sesija
    {
        Id = id,
        Status = status,
        UredjajId = uredjajId,
        KorisnikId = korisnikId,
        VrijemePocetka = DateTime.Now.AddHours(-2),
        VrijemeZavrsetka = kraj ?? DateTime.Now.AddHours(2)
    };

    private void Dodaj(params Sesija[] sesije)
    {
        using var kontekst = NoviKontekst();
        kontekst.Sesija.AddRange(sesije);
        kontekst.SaveChanges();
    }

    private void DodajLoyalty(string korisnikId, int bodovi)
    {
        using var kontekst = NoviKontekst();
        kontekst.LoyaltyProgram.Add(new LoyaltyProgram { KorisnikId = korisnikId, UkupniBodovi = bodovi, PopustPoBodu = 0.10 });
        kontekst.SaveChanges();
    }

    private Sesija Ucitaj(int id)
    {
        using var kontekst = NoviKontekst();
        return kontekst.Sesija.Single(s => s.Id == id);
    }

    private int BrojSesija()
    {
        using var kontekst = NoviKontekst();
        return kontekst.Sesija.Count();
    }

    private LoyaltyProgram UcitajLoyalty(string korisnikId)
    {
        using var kontekst = NoviKontekst();
        return kontekst.LoyaltyProgram.Single(l => l.KorisnikId == korisnikId);
    }

    private void DodajPC(int id)
    {
        using var kontekst = NoviKontekst();
        kontekst.PC.Add(new PC { Id = id, Naziv = "PC " + id, Model = "Model", CijenaPoSatu = 5 });
        kontekst.SaveChanges();
    }

    private void DodajPlayStationIXBox(int playStationId, int xboxId)
    {
        using var kontekst = NoviKontekst();
        kontekst.PlayStation.Add(new PlayStation { Id = playStationId, Naziv = "PS", CijenaPoSatu = 6 });
        kontekst.XBox.Add(new XBox { Id = xboxId, Naziv = "Xbox", CijenaPoSatu = 7 });
        kontekst.SaveChanges();
    }

    private void DodajKorisnika(string id)
    {
        using var kontekst = NoviKontekst();
        kontekst.Korisnik.Add(new Korisnik { Id = id, UserName = id, Ime = "Ime", Prezime = "Prezime" });
        kontekst.SaveChanges();
    }

    private void DodajRezervaciju(int id, int uredjajId, string korisnikId)
    {
        using var kontekst = NoviKontekst();
        kontekst.Rezervacija.Add(new Rezervacija
        {
            Id = id,
            UredjajId = uredjajId,
            KorisnikId = korisnikId,
            Status = StatusRezervacije.Aktivna,
            VrijemeOd = DateTime.Now.AddHours(-1),
            VrijemeDo = DateTime.Now.AddHours(1)
        });
        kontekst.SaveChanges();
    }

    private Rezervacija UcitajRezervaciju(int id)
    {
        using var kontekst = NoviKontekst();
        return kontekst.Rezervacija.Single(r => r.Id == id);
    }

    private static int BrojElemenataUViewData(ViewResult view, string kljuc) =>
        ((System.Collections.IEnumerable)view.ViewData[kljuc]!).Cast<object>().Count();

    private static void ProvjeriPreusmjerenje(IActionResult rezultat, string akcija)
    {
        var preusmjerenje = rezultat as RedirectToActionResult;
        Assert.IsNotNull(preusmjerenje);
        Assert.AreEqual(akcija, preusmjerenje.ActionName);
    }

    // ---------------- Index ----------------

    [TestMethod]
    public async Task Index_AktivnaSesijaSaProslimKrajem_PostavljaStatusZavrsena()
    {
        Dodaj(NovaSesija(1, kraj: DateTime.Now.AddHours(-1)));

        var rezultat = await NoviKontroler().Index();

        Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        Assert.AreEqual(StatusSesije.Zavrsena, Ucitaj(1).Status);
    }

    [TestMethod]
    public async Task Index_AktivnaSesijaSaBuducimKrajem_OstajeAktivna()
    {
        Dodaj(NovaSesija(1, kraj: DateTime.Now.AddHours(1)));

        await NoviKontroler().Index();

        Assert.AreEqual(StatusSesije.Aktivna, Ucitaj(1).Status);
    }

    // ---------------- Details ----------------

    [TestMethod]
    [DataRow(null)]
    [DataRow(999)]
    public async Task Details_NullIliNepostojeciId_VracaNotFound(int? id)
    {
        var rezultat = await NoviKontroler().Details(id);

        Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
    }

    // ---------------- Create (GET) ----------------

    [TestMethod]
    public async Task CreateGet_PraznaBaza_VracaViewSaNovomSesijom()
    {
        var rezultat = await NoviKontroler().Create() as ViewResult;

        Assert.IsNotNull(rezultat);
        Assert.IsInstanceOfType(rezultat.Model, typeof(Sesija));
    }

    // ---------------- Create (POST) ----------------

    [TestMethod]
    [DataRow(null, "k1", false)]
    [DataRow(1, null, false)]
    [DataRow(1, "", false)]
    public async Task CreatePost_NedostajuciPodaci_PreusmjeravaNaCreate(int? uredjajId, string? korisnikId, bool anonimniGost)
    {
        var rezultat = await NoviKontroler().Create(null, uredjajId, korisnikId, anonimniGost, "23:59:59");

        ProvjeriPreusmjerenje(rezultat, "Create");
    }

    [TestMethod]
    public async Task CreatePost_VrijemeKrajaURanijeVrijeme_PreusmjeravaNaCreate()
    {
        var rezultat = await NoviKontroler().Create(null, 1, "k1", false, "00:00:00");

        ProvjeriPreusmjerenje(rezultat, "Create");
    }

    [TestMethod]
    public async Task CreatePost_ValidniPodaci_DodajeSesijuIPreusmjeravaNaIndex()
    {
        var rezultat = await NoviKontroler().Create(null, 1, "k1", false, "23:59:59");

        ProvjeriPreusmjerenje(rezultat, "Index");
        Assert.AreEqual(1, BrojSesija());
    }

    [TestMethod]
    public async Task CreatePost_AnonimniGost_KorisnikIdJeNull()
    {
        await NoviKontroler().Create(null, 1, "k1", true, "23:59:59");

        Assert.IsNull(Ucitaj(1).KorisnikId);
        Assert.IsTrue(Ucitaj(1).AnonimniGost);
    }

    [TestMethod]
    public async Task CreatePost_NepostojecaRezervacija_VracaNotFound()
    {
        var rezultat = await NoviKontroler().Create(99, null, null, false, "23:59:59");

        Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task CreatePost_NeispravanFormatVremena_BacaFormatException()
    {
        await Assert.ThrowsExceptionAsync<FormatException>(
            () => NoviKontroler().Create(null, 1, "k1", false, "nije-vrijeme"));
    }

    // ---------------- Edit (GET) ----------------

    [TestMethod]
    [DataRow(null)]
    [DataRow(999)]
    public async Task EditGet_NullIliNepostojeciId_VracaNotFound(int? id)
    {
        var rezultat = await NoviKontroler().Edit(id);

        Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task EditGet_PostojecaSesija_VracaViewSaSesijom()
    {
        Dodaj(NovaSesija(1));

        var rezultat = await NoviKontroler().Edit(1) as ViewResult;

        Assert.IsNotNull(rezultat);
        Assert.AreEqual(1, ((Sesija)rezultat.Model!).Id);
    }

    // ---------------- Edit (POST) ----------------

    [TestMethod]
    public async Task EditPost_RazlicitIdOdSesije_VracaNotFound()
    {
        var rezultat = await NoviKontroler().Edit(2, NovaSesija(1));

        Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task EditPost_ValidnaSesija_SpasavaPromjeneIPreusmjeravaNaIndex()
    {
        Dodaj(NovaSesija(1, korisnikId: "k1"));

        var rezultat = await NoviKontroler().Edit(1, NovaSesija(1, StatusSesije.Pauzirana, korisnikId: null));

        ProvjeriPreusmjerenje(rezultat, "Index");
        Assert.AreEqual(StatusSesije.Pauzirana, Ucitaj(1).Status);
        Assert.IsTrue(Ucitaj(1).AnonimniGost);
    }

    [TestMethod]
    public async Task EditPost_NevalidanModel_VracaViewSaIstomSesijom()
    {
        var kontroler = NoviKontroler();
        kontroler.ModelState.AddModelError("Status", "Greska");
        var sesija = NovaSesija(1);

        var rezultat = await kontroler.Edit(1, sesija) as ViewResult;

        Assert.IsNotNull(rezultat);
        Assert.AreSame(sesija, rezultat.Model);
    }

    [TestMethod]
    public async Task EditPost_SesijaNePostojiUBazi_VracaNotFound()
    {
        var rezultat = await NoviKontroler().Edit(5, NovaSesija(5));

        Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
    }

    // ---------------- Edit (POST) sa zamjenskim objektom (Moq) ----------------

    private Mock<ApplicationDbContext> NapraviKontekstKojiBacaKonkurenciju()
    {
        var zamjenskiKontekst = new Mock<ApplicationDbContext>(_options) { CallBase = true };
        zamjenskiKontekst
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        return zamjenskiKontekst;
    }

    [TestMethod]
    [TestCategory("Mock")]
    public async Task EditPost_KonkurentnaIzmjenaISesijaPostoji_PropagiraDbUpdateConcurrencyException()
    {
        Dodaj(NovaSesija(1));
        var zamjenskiKontekst = NapraviKontekstKojiBacaKonkurenciju();
        var kontroler = new SesijaController(zamjenskiKontekst.Object);

        await Assert.ThrowsExceptionAsync<DbUpdateConcurrencyException>(
            () => kontroler.Edit(1, NovaSesija(1)));

        zamjenskiKontekst.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Mock")]
    public async Task EditPost_KonkurentnaIzmjenaISesijaObrisana_VracaNotFound()
    {
        var zamjenskiKontekst = NapraviKontekstKojiBacaKonkurenciju();
        var kontroler = new SesijaController(zamjenskiKontekst.Object);

        var rezultat = await kontroler.Edit(1, NovaSesija(1));

        Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
    }

    // ---------------- Delete ----------------

    [TestMethod]
    [DataRow(null)]
    [DataRow(999)]
    public async Task DeleteGet_NullIliNepostojeciId_VracaNotFound(int? id)
    {
        var rezultat = await NoviKontroler().Delete(id);

        Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task DeleteConfirmed_PostojecaSesija_BriseSesijuIPreusmjeravaNaIndex()
    {
        Dodaj(NovaSesija(1));

        var rezultat = await NoviKontroler().DeleteConfirmed(1);

        ProvjeriPreusmjerenje(rezultat, "Index");
        Assert.AreEqual(0, BrojSesija());
    }

    [TestMethod]
    public async Task DeleteConfirmed_NepostojecaSesija_PreusmjeravaNaIndexBezGreske()
    {
        var rezultat = await NoviKontroler().DeleteConfirmed(999);

        ProvjeriPreusmjerenje(rezultat, "Index");
    }

    // ---------------- EndSession ----------------

    [TestMethod]
    public async Task EndSession_NepostojecaSesija_VracaNotFound()
    {
        var rezultat = await NoviKontroler().EndSession(999);

        Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task EndSession_KorisnikBezLoyaltyja_KreiraLoyaltyDesetBodova()
    {
        Dodaj(NovaSesija(1, korisnikId: "k1"));

        var rezultat = await NoviKontroler().EndSession(1);

        ProvjeriPreusmjerenje(rezultat, "Index");
        Assert.AreEqual(StatusSesije.Zavrsena, Ucitaj(1).Status);
        Assert.AreEqual(10, UcitajLoyalty("k1").UkupniBodovi);
    }

    [TestMethod]
    public async Task EndSession_KorisnikSaLoyaltyjem_DodajeDesetBodova()
    {
        Dodaj(NovaSesija(1, korisnikId: "k1"));
        DodajLoyalty("k1", 5);

        await NoviKontroler().EndSession(1);

        Assert.AreEqual(15, UcitajLoyalty("k1").UkupniBodovi);
    }

    [TestMethod]
    public async Task EndSession_AnonimnaSesija_ZavrsavaSesijuBezBodova()
    {
        Dodaj(NovaSesija(1, korisnikId: null));

        await NoviKontroler().EndSession(1);

        using var kontekst = NoviKontekst();
        Assert.AreEqual(StatusSesije.Zavrsena, Ucitaj(1).Status);
        Assert.AreEqual(0, kontekst.LoyaltyProgram.Count());
    }

    // ---------------- Testovi koji trebaju Uredjaj / Korisnik / Rezervaciju ----------------

    [TestMethod]
    public async Task Details_PostojecaSesija_VracaViewSaSesijom()
    {
        DodajPC(1);
        Dodaj(NovaSesija(1));

        var rezultat = await NoviKontroler().Details(1) as ViewResult;

        Assert.IsNotNull(rezultat);
        Assert.AreEqual(1, ((Sesija)rezultat.Model!).Id);
    }

    [TestMethod]
    public async Task DeleteGet_PostojecaSesija_VracaViewSaSesijom()
    {
        DodajPC(1);
        Dodaj(NovaSesija(1));

        var rezultat = await NoviKontroler().Delete(1) as ViewResult;

        Assert.IsNotNull(rezultat);
        Assert.AreEqual(1, ((Sesija)rezultat.Model!).Id);
    }

    [TestMethod]
    public async Task CreateGet_UredjajZauzetAktivnomSesijom_VracaSamoSlobodneUredjaje()
    {
        DodajPC(1);
        DodajPC(2);
        Dodaj(NovaSesija(1, uredjajId: 2));

        var rezultat = (ViewResult)await NoviKontroler().Create();

        Assert.AreEqual(1, BrojElemenataUViewData(rezultat, "Uredjaji"));
    }

    [TestMethod]
    public async Task CreateGet_UredjajSaAktivnomRezervacijom_NijeUListiSlobodnih()
    {
        DodajPC(1);
        DodajKorisnika("k1");
        DodajRezervaciju(1, 1, "k1");

        var rezultat = (ViewResult)await NoviKontroler().Create();

        Assert.AreEqual(0, BrojElemenataUViewData(rezultat, "Uredjaji"));
        Assert.AreEqual(1, BrojElemenataUViewData(rezultat, "Rezervacije"));
    }

    [TestMethod]
    public async Task CreatePost_PostojecaRezervacija_KreiraSesijuIZavrsavaRezervaciju()
    {
        DodajPC(1);
        DodajKorisnika("k1");
        DodajRezervaciju(1, 1, "k1");

        var rezultat = await NoviKontroler().Create(1, null, null, false, "23:59:59");

        ProvjeriPreusmjerenje(rezultat, "Index");
        Assert.AreEqual("k1", Ucitaj(1).KorisnikId);
        Assert.AreEqual(StatusRezervacije.Zavrsena, UcitajRezervaciju(1).Status);
    }

    [TestMethod]
    public async Task CreateGet_PCPlayStationIXBoxSlobodni_VracaSveTriUredjaja()
    {
        DodajPC(1);
        DodajPlayStationIXBox(2, 3);

        var rezultat = (ViewResult)await NoviKontroler().Create();

        Assert.AreEqual(3, BrojElemenataUViewData(rezultat, "Uredjaji"));
    }
}
