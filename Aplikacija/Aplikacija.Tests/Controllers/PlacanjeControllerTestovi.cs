using System.Reflection;
using Aplikacija.Controllers;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Aplikacija.Tests.Controllers;

[TestClass, TestCategory("Kontroler")]
public class PlacanjeControllerTestovi
{
    private string _baza = null!;

    [TestInitialize]
    public void Postavi()
    {
        // Svaki test dobija svoju praznu in-memory bazu (zamjena za pravu bazu).
        _baza = TestDb.NewName();
    }

    // ---------------- pomocne metode ----------------

    private ApplicationDbContext NoviKontekst() => TestDb.Create(_baza);

    private PlacanjeController NoviKontroler() => new(NoviKontekst());

    private static Placanje NovoPlacanje(int id, double iznos = 10, string korisnikId = "k1",
        int rezervacijaId = 1, int? sesijaId = null, string? metoda = "Cash") => new()
    {
        Id = id, Iznos = iznos, KorisnikId = korisnikId, RezervacijaId = rezervacijaId,
        SesijaId = sesijaId, MetodaPlacanja = metoda!, DatumPlacanja = new DateTime(2026, 11, 15, 12, 0, 0)
    };

    // Korisnik k1, PC 10 i rezervacija sa zadanim Id-em, kako bi Include upiti imali sve obavezne veze.
    private void DodajOsnovnePodatke(params int[] rezervacije)
    {
        using var kontekst = NoviKontekst();
        kontekst.SeedKorisnik("k1");
        kontekst.SeedDevice("PC", 10, 5);
        foreach (int id in rezervacije.DefaultIfEmpty(1))
            kontekst.SeedRezervacija(id, "k1", 10, new DateTime(2026, 11, 15, 10, 0, 0),
                new DateTime(2026, 11, 15, 12, 0, 0), StatusRezervacije.Zavrsena);
    }

    private void Dodaj(params object[] objekti)
    {
        using var kontekst = NoviKontekst();
        kontekst.AddRange(objekti);
        kontekst.SaveChanges();
    }

    private Placanje? Ucitaj(int id)
    {
        using var kontekst = NoviKontekst();
        return kontekst.Placanje.AsNoTracking().SingleOrDefault(p => p.Id == id);
    }

    private int BrojPlacanja()
    {
        using var kontekst = NoviKontekst();
        return kontekst.Placanje.Count();
    }

    private Mock<ApplicationDbContext> ZamjenskiKontekstKojiBaca(Exception izuzetak)
    {
        var zamjenski = new Mock<ApplicationDbContext>(TestDb.Options(_baza)) { CallBase = true };
        zamjenski.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(izuzetak);
        return zamjenski;
    }

    private static SelectList IzborKorisnika(Controller kontroler) =>
        (SelectList)kontroler.ViewData["KorisnikId"]!;

    // ---------------- Autorizacija ----------------

    [TestMethod, TestCategory("Autorizacija")]
    public void Kontroler_Atributi_DozvoljavaSamoAdministratoruIZaposleniku()
    {
        var atribut = typeof(PlacanjeController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.IsNotNull(atribut);
        Assert.AreEqual("Administrator,Employee", atribut.Roles);
    }

    [TestMethod, TestCategory("Autorizacija")]
    [DataRow(nameof(PlacanjeController.Create))]
    [DataRow(nameof(PlacanjeController.Edit))]
    [DataRow(nameof(PlacanjeController.Delete))]
    [DataRow(nameof(PlacanjeController.DeleteConfirmed))]
    public void IzmjenaPodataka_SveVarijanteAkcije_DozvoljeneSamoAdministratoru(string akcija)
    {
        var metode = typeof(PlacanjeController).GetMethods().Where(m => m.Name == akcija).ToList();

        var uloge = metode.Select(m => m.GetCustomAttribute<AuthorizeAttribute>()?.Roles).ToList();

        Assert.IsTrue(metode.Count > 0);
        CollectionAssert.AreEqual(Enumerable.Repeat("Administrator", metode.Count).ToList(), uloge);
    }

    [TestMethod, TestCategory("Autorizacija")]
    [DataRow(nameof(PlacanjeController.Create))]
    [DataRow(nameof(PlacanjeController.Edit))]
    [DataRow(nameof(PlacanjeController.DeleteConfirmed))]
    public void PostAkcije_Atributi_ZahtijevajuAntiForgeryToken(string akcija)
    {
        var post = typeof(PlacanjeController).GetMethods()
            .Single(m => m.Name == akcija && m.GetCustomAttribute<HttpPostAttribute>() != null);

        var token = post.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>();

        Assert.IsNotNull(token);
    }

    // ---------------- Index ----------------

    [TestMethod]
    public async Task Index_PraznaBaza_VracaPraznuListu()
    {
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Index();

        Assert.AreEqual(0, Provjere.ModelPrikaza<List<Placanje>>(rezultat).Count);
    }

    [TestMethod]
    public async Task Index_PostojecaPlacanja_VracaSvaPlacanjaSaUcitanimVezama()
    {
        DodajOsnovnePodatke(1, 2);
        Dodaj(new Sesija { Id = 5, UredjajId = 10, KorisnikId = "k1", VrijemePocetka = DateTime.Now, VrijemeZavrsetka = DateTime.Now });
        Dodaj(NovoPlacanje(1, rezervacijaId: 1), NovoPlacanje(2, rezervacijaId: 2, sesijaId: 5));
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Index();

        var model = Provjere.ModelPrikaza<List<Placanje>>(rezultat);
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, model.Select(p => p.Id).ToArray());
        Assert.IsTrue(model.All(p => p.Korisnik.Id == "k1"));
        Assert.IsTrue(model.All(p => p.Rezervacija.Id == p.RezervacijaId));
        Assert.IsNull(model.Single(p => p.Id == 1).Sesija);
        Assert.AreEqual(5, model.Single(p => p.Id == 2).Sesija!.Id);
    }

    // ---------------- Details ----------------

    [TestMethod]
    [DataRow(null), DataRow(0), DataRow(-1), DataRow(999)]
    public async Task Details_NullIliNepostojeciId_VracaNotFound(int? id)
    {
        DodajOsnovnePodatke();
        Dodaj(NovoPlacanje(1));
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Details(id);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Details_PostojeciId_VracaPlacanjeSaKorisnikomIRezervacijom()
    {
        DodajOsnovnePodatke();
        Dodaj(NovoPlacanje(1, iznos: 22.5));
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Details(1);

        var placanje = Provjere.ModelPrikaza<Placanje>(rezultat);
        Assert.AreEqual(22.5, placanje.Iznos, 0.0001);
        Assert.AreEqual("k1@test.ba", placanje.Korisnik.Email);
        Assert.AreEqual(1, placanje.Rezervacija.Id);
    }

    // ---------------- Create (GET) ----------------

    [TestMethod]
    public void CreateGet_PostojeciKorisnici_VracaPrikazIIzborPoEmailu()
    {
        using (var kontekst = NoviKontekst())
        {
            kontekst.SeedKorisnik("k1");
            kontekst.SeedKorisnik("k2");
        }
        var kontroler = NoviKontroler();

        var rezultat = kontroler.Create();

        Assert.IsNull(Provjere.Prikaz(rezultat).Model);
        var izbor = IzborKorisnika(kontroler);
        Assert.AreEqual("Id", izbor.DataValueField);
        Assert.AreEqual("Email", izbor.DataTextField);
        CollectionAssert.AreEquivalent(new[] { "k1@test.ba", "k2@test.ba" }, izbor.Select(s => s.Text).ToArray());
    }

    [TestMethod]
    public void CreateGet_BezKorisnika_VracaPrazanIzbor()
    {
        var kontroler = NoviKontroler();

        kontroler.Create();

        Assert.AreEqual(0, IzborKorisnika(kontroler).Count());
    }

    // ---------------- Create (POST) ----------------

    [TestMethod]
    public async Task CreatePost_IspravnoPlacanje_SnimaIPreusmjeravaNaIndex()
    {
        DodajOsnovnePodatke();
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Create(NovoPlacanje(0, iznos: 35));

        Provjere.Preusmjerenje(rezultat);
        var snimljeno = Ucitaj(1)!;
        Assert.AreEqual(35, snimljeno.Iznos, 0.0001);
        Assert.AreEqual("k1", snimljeno.KorisnikId);
        Assert.AreEqual("Cash", snimljeno.MetodaPlacanja);
    }

    [TestMethod]
    public async Task CreatePost_NeispravanModel_VracaIstiPrikazBezSnimanja()
    {
        DodajOsnovnePodatke();
        var kontroler = NoviKontroler();
        kontroler.ModelState.AddModelError(nameof(Placanje.Iznos), "Amount must be greater than 0!");
        var placanje = NovoPlacanje(0, iznos: -5);

        var rezultat = await kontroler.Create(placanje);

        Assert.AreSame(placanje, Provjere.ModelPrikaza<Placanje>(rezultat));
        Assert.AreEqual(0, BrojPlacanja());
        var izbor = IzborKorisnika(kontroler);
        Assert.AreEqual("k1", izbor.SelectedValue);
        CollectionAssert.AreEqual(new[] { "k1" }, izbor.Select(s => s.Value).ToArray());
    }

    [TestMethod]
    public async Task CreatePost_PlacanjeBezMetodePlacanja_BacaDbUpdateException()
    {
        // Create povezuje samo "Id,Iznos,KorisnikId", pa obavezna MetodaPlacanja ostaje prazna.
        DodajOsnovnePodatke();
        var kontroler = NoviKontroler();

        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => kontroler.Create(NovoPlacanje(0, metoda: null)));

        Assert.AreEqual(0, BrojPlacanja());
    }

    [TestMethod, TestCategory("ZamjenskiObjekat")]
    public async Task CreatePost_GreskaBazePriSnimanju_PropagiraIzuzetak()
    {
        var zamjenski = ZamjenskiKontekstKojiBaca(new DbUpdateException("Baza nije dostupna"));
        var kontroler = new PlacanjeController(zamjenski.Object);

        var izuzetak = await Assert.ThrowsExceptionAsync<DbUpdateException>(() => kontroler.Create(NovoPlacanje(0)));

        Assert.AreEqual("Baza nije dostupna", izuzetak.Message);
        zamjenski.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------------- Edit (GET) ----------------

    [TestMethod]
    [DataRow(null), DataRow(0), DataRow(999)]
    public async Task EditGet_NullIliNepostojeciId_VracaNotFound(int? id)
    {
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Edit(id);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task EditGet_PostojeciId_VracaPlacanjeIOznacenogKorisnika()
    {
        DodajOsnovnePodatke();
        Dodaj(NovoPlacanje(1));
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Edit(1);

        Assert.AreEqual(1, Provjere.ModelPrikaza<Placanje>(rezultat).Id);
        var izbor = IzborKorisnika(kontroler);
        Assert.AreEqual("k1", izbor.SelectedValue);
        Assert.AreEqual("Email", izbor.DataTextField);
    }

    // ---------------- Edit (POST) ----------------

    [TestMethod]
    [DataRow(2), DataRow(0), DataRow(-1)]
    public async Task EditPost_IdNeOdgovaraPlacanju_VracaNotFoundBezIzmjene(int id)
    {
        DodajOsnovnePodatke();
        Dodaj(NovoPlacanje(1, iznos: 10));
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Edit(id, NovoPlacanje(1, iznos: 99));

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(10, Ucitaj(1)!.Iznos, 0.0001);
    }

    [TestMethod]
    public async Task EditPost_IspravneIzmjene_SnimaIPreusmjeravaNaIndex()
    {
        DodajOsnovnePodatke();
        Dodaj(NovoPlacanje(1, iznos: 10));
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Edit(1, NovoPlacanje(1, iznos: 45.5, metoda: "Card"));

        Provjere.Preusmjerenje(rezultat);
        var snimljeno = Ucitaj(1)!;
        Assert.AreEqual(45.5, snimljeno.Iznos, 0.0001);
        Assert.AreEqual("Card", snimljeno.MetodaPlacanja);
    }

    [TestMethod]
    public async Task EditPost_NeispravanModel_VracaPrikazBezIzmjene()
    {
        DodajOsnovnePodatke();
        Dodaj(NovoPlacanje(1, iznos: 10));
        var kontroler = NoviKontroler();
        kontroler.ModelState.AddModelError(nameof(Placanje.Iznos), "Amount must be greater than 0!");
        var izmjena = NovoPlacanje(1, iznos: 0);

        var rezultat = await kontroler.Edit(1, izmjena);

        Assert.AreSame(izmjena, Provjere.ModelPrikaza<Placanje>(rezultat));
        Assert.AreEqual("k1", IzborKorisnika(kontroler).SelectedValue);
        Assert.AreEqual(10, Ucitaj(1)!.Iznos, 0.0001);
    }

    [TestMethod]
    public async Task EditPost_PlacanjeObrisanoUMeduvremenu_VracaNotFound()
    {
        // In-memory baza baca DbUpdateConcurrencyException kada se azurira nepostojeci red.
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Edit(1, NovoPlacanje(1));

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, BrojPlacanja());
    }

    [TestMethod, TestCategory("ZamjenskiObjekat")]
    public async Task EditPost_KonkurentnaIzmjenaPostojecegPlacanja_PropagiraDbUpdateConcurrencyException()
    {
        DodajOsnovnePodatke();
        Dodaj(NovoPlacanje(1, iznos: 10));
        var zamjenski = ZamjenskiKontekstKojiBaca(new DbUpdateConcurrencyException());
        var kontroler = new PlacanjeController(zamjenski.Object);

        await Assert.ThrowsExceptionAsync<DbUpdateConcurrencyException>(() => kontroler.Edit(1, NovoPlacanje(1, iznos: 99)));

        zamjenski.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.AreEqual(10, Ucitaj(1)!.Iznos, 0.0001);
    }

    // ---------------- Delete (GET) ----------------

    [TestMethod]
    [DataRow(null), DataRow(0), DataRow(999)]
    public async Task DeleteGet_NullIliNepostojeciId_VracaNotFound(int? id)
    {
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Delete(id);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task DeleteGet_PostojeciId_VracaPotvrduSaKorisnikomBezBrisanja()
    {
        DodajOsnovnePodatke();
        Dodaj(NovoPlacanje(1));
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.Delete(1);

        var placanje = Provjere.ModelPrikaza<Placanje>(rezultat);
        Assert.AreEqual("k1", placanje.Korisnik.Id);
        Assert.AreEqual(1, BrojPlacanja());
    }

    // ---------------- DeleteConfirmed ----------------

    [TestMethod]
    public async Task DeleteConfirmed_PostojecePlacanje_BriseSamoTajZapisIPreusmjerava()
    {
        DodajOsnovnePodatke(1, 2);
        Dodaj(NovoPlacanje(1, rezervacijaId: 1), NovoPlacanje(2, rezervacijaId: 2));
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.DeleteConfirmed(1);

        Provjere.Preusmjerenje(rezultat);
        Assert.IsNull(Ucitaj(1));
        Assert.IsNotNull(Ucitaj(2));
    }

    [TestMethod]
    public async Task DeleteConfirmed_NepostojecePlacanje_PreusmjeravaBezPromjena()
    {
        DodajOsnovnePodatke();
        Dodaj(NovoPlacanje(1));
        var kontroler = NoviKontroler();

        var rezultat = await kontroler.DeleteConfirmed(999);

        Provjere.Preusmjerenje(rezultat);
        Assert.AreEqual(1, BrojPlacanja());
    }

    [TestMethod, TestCategory("ZamjenskiObjekat")]
    public async Task DeleteConfirmed_GreskaBazePriBrisanju_PropagiraIzuzetak()
    {
        DodajOsnovnePodatke();
        Dodaj(NovoPlacanje(1));
        var zamjenski = ZamjenskiKontekstKojiBaca(new DbUpdateException("Zapis je zakljucan"));
        var kontroler = new PlacanjeController(zamjenski.Object);

        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => kontroler.DeleteConfirmed(1));

        Assert.AreEqual(1, BrojPlacanja());
    }
}
