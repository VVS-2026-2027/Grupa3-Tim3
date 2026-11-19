using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using Aplikacija.Controllers;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Aplikacija.Tests.Controllers;

[TestClass]
public class HomeControllerTests
{
    private const string PorukaUspjeha = "Takmičenja su učitana iz PandaScore vanjskog sistema.";

    private static HomeController Napravi(TestBaza baza, ClaimsPrincipal? principal = null,
        IHttpClientFactory? httpFactory = null, IConfiguration? konfiguracija = null,
        string? traceId = null)
    {
        return new HomeController(
            baza.Context,
            TestObjekti.NapraviUserManager(baza.Context),
            null!, 
            httpFactory!,
            konfiguracija!)
        {
            ControllerContext = TestObjekti.KontekstKontrolera(principal, traceId)
        };
    }

    private static HomeController NapraviZaUcitavanje(TestBaza baza, LazniHttpHandler handler)
    {
        var kontroler = Napravi(baza, TestObjekti.Principal("admin-1", "Administrator"),
            new LazniHttpClientFactory(handler),
            TestObjekti.Konfiguracija("https://api.test.local/matches", "tajni-token"));
        kontroler.TempData = TestObjekti.NoviTempData();
        return kontroler;
    }

    private static string Json(string naziv, string? pocetak, string? igra = "CS2")
    {
        var begin = pocetak is null ? "null" : $"\"{pocetak}\"";
        var videogame = igra is null ? "null" : $"{{\"name\":\"{igra}\"}}";
        return $"{{\"name\":\"{naziv}\",\"begin_at\":{begin},\"videogame\":{videogame}}}";
    }

    //  Index: osnovno ponasanje i takmicenja 

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_Anoniman_VracaPrikazBezPodatakaOUlogama()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza);

        var rezultat = await kontroler.Index();

        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.IsFalse(kontroler.ViewData.ContainsKey("PendingUsers"));
        Assert.IsFalse(kontroler.ViewData.ContainsKey("ClanarinaAktivna"));
        Assert.IsFalse(kontroler.ViewData.ContainsKey("AktivneNarudzbe"));
        Assert.IsTrue(kontroler.ViewData.ContainsKey("Takmicenja"));
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_ProslaTakmicenja_BrisuSeIPrikazujuSeSamoDanasnjaIBuduca()
    {
        
        var danas = DateTime.Today;
        using var baza = new TestBaza();
        baza.Dodaj(
            new Takmicenje { Naziv = "Prosli", Igra = "CS2", Datum = danas.AddDays(-1) },
            new Takmicenje { Naziv = "Danas", Igra = "CS2", Datum = danas },
            new Takmicenje { Naziv = "Sutra", Igra = "Dota", Datum = danas.AddDays(1) });
        var kontroler = Napravi(baza);
        
        var rezultat = await kontroler.Index();
        
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        var prikazana = (List<Takmicenje>)kontroler.ViewData["Takmicenja"]!;
        CollectionAssert.AreEqual(new[] { "Danas", "Sutra" }, prikazana.Select(t => t.Naziv).ToArray());
        Assert.AreEqual(1, baza.Context.BrojSnimanja);

        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(2, await provjera.Takmicenje.CountAsync());
        Assert.IsFalse(await provjera.Takmicenje.AnyAsync(t => t.Naziv == "Prosli"));
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_NemaProslihTakmicenja_NeSnimaPromjene()
    {
        using var baza = new TestBaza();
        baza.Dodaj(new Takmicenje { Naziv = "Sutra", Igra = "CS2", Datum = DateTime.Today.AddDays(1) });
        var kontroler = Napravi(baza);

        await kontroler.Index();

        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_BezTakmicenja_PrikazujePrazanSpisak()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza);

        await kontroler.Index();

        Assert.AreEqual(0, ((List<Takmicenje>)kontroler.ViewData["Takmicenja"]!).Count);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_ViseOdDesetTakmicenja_PrikazujeNajranijihDeset()
    {
        // Arrange: 12 buducih takmicenja, upisanih obrnutim redoslijedom
        var danas = DateTime.Today;
        using var baza = new TestBaza();
        var takmicenja = Enumerable.Range(1, 12).Reverse()
            .Select(i => (object)new Takmicenje { Naziv = $"T{i}", Igra = "CS2", Datum = danas.AddDays(i) })
            .ToArray();
        baza.Dodaj(takmicenja);
        var kontroler = Napravi(baza);

        await kontroler.Index();

        var prikazana = (List<Takmicenje>)kontroler.ViewData["Takmicenja"]!;
        var ocekivano = Enumerable.Range(1, 10).Select(i => $"T{i}").ToArray();
        CollectionAssert.AreEqual(ocekivano, prikazana.Select(t => t.Naziv).ToArray());
    }

    //  Index: administrator 

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_Administrator_VidiSamoKorisnikeUUloziUserCijaClanarinaNijeAktivna()
    {
        // Arrange
        using var baza = new TestBaza();
        baza.Dodaj(
            TestObjekti.Uloga("User"), TestObjekti.Uloga("Employee"),
            TestObjekti.NoviKorisnik("na-cekanju", clanarinaAktivna: false),
            TestObjekti.NoviKorisnik("aktivan", clanarinaAktivna: true),
            TestObjekti.NoviKorisnik("zaposlenik", clanarinaAktivna: false),
            TestObjekti.DodjelaUloge("na-cekanju", "User"),
            TestObjekti.DodjelaUloge("aktivan", "User"),
            TestObjekti.DodjelaUloge("zaposlenik", "Employee"));
        var kontroler = Napravi(baza, TestObjekti.Principal("admin-1", "Administrator"));
        
        var rezultat = await kontroler.Index();
        
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        var naCekanju = (List<Korisnik>)kontroler.ViewData["PendingUsers"]!;
        Assert.AreEqual(1, naCekanju.Count);
        Assert.AreEqual("na-cekanju", naCekanju[0].Id);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_AdministratorBezKorisnika_PendingUsersJePrazna()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, TestObjekti.Principal("admin-1", "Administrator"));

        await kontroler.Index();

        Assert.AreEqual(0, ((List<Korisnik>)kontroler.ViewData["PendingUsers"]!).Count);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_Administrator_DobijaListuAktivnihNarudzbi()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, TestObjekti.Principal("admin-1", "Administrator"));

        await kontroler.Index();

        Assert.IsTrue(kontroler.ViewData.ContainsKey("AktivneNarudzbe"));
        Assert.IsInstanceOfType<List<Narudzba>>(kontroler.ViewData["AktivneNarudzbe"]);
        Assert.AreEqual(0, ((List<Narudzba>)kontroler.ViewData["AktivneNarudzbe"]!).Count);
    }

    //  Index: zaposlenik 

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_Employee_DobijaAktivneNarudzbeAliNePendingUsers()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, TestObjekti.Principal("emp-1", "Employee"));

        await kontroler.Index();

        Assert.IsTrue(kontroler.ViewData.ContainsKey("AktivneNarudzbe"));
        Assert.IsFalse(kontroler.ViewData.ContainsKey("PendingUsers"));
        Assert.IsFalse(kontroler.ViewData.ContainsKey("ClanarinaAktivna"));
    }

    //  Index: obicni korisnik 

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_UserSaAktivnomClanarinomILoyalty_PrikazujeBodove()
    {
        
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("u1", clanarinaAktivna: true),
            new LoyaltyProgram { KorisnikId = "u1", UkupniBodovi = 120, PopustPoBodu = 0.04 });
        var kontroler = Napravi(baza, TestObjekti.Principal("u1", "User"));
        
        var rezultat = await kontroler.Index();
       
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.IsTrue((bool)kontroler.ViewData["ClanarinaAktivna"]!);
        Assert.AreEqual(120, kontroler.ViewData["LoyaltyPoints"]);
        Assert.IsFalse((bool)kontroler.ViewData["ImaAktivnuSesiju"]!);
        Assert.IsFalse(kontroler.ViewData.ContainsKey("AktivneNarudzbe"));
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_UserSaAktivnomClanarinomBezLoyalty_PrikazujeNulaBodova()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("u1", clanarinaAktivna: true));
        var kontroler = Napravi(baza, TestObjekti.Principal("u1", "User"));

        await kontroler.Index();

        Assert.AreEqual(0, kontroler.ViewData["LoyaltyPoints"]);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_UserBezAktivneClanarine_NePrikazujeBodoveIAkoPostoje()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("u1", clanarinaAktivna: false),
            new LoyaltyProgram { KorisnikId = "u1", UkupniBodovi = 120, PopustPoBodu = 0.04 });
        var kontroler = Napravi(baza, TestObjekti.Principal("u1", "User"));

        await kontroler.Index();

        Assert.IsFalse((bool)kontroler.ViewData["ClanarinaAktivna"]!);
        Assert.IsFalse(kontroler.ViewData.ContainsKey("LoyaltyPoints"));
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_UserBezSesija_NemaAktivnuSesiju()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("u1", clanarinaAktivna: true));
        var kontroler = Napravi(baza, TestObjekti.Principal("u1", "User"));

        await kontroler.Index();

        Assert.IsFalse((bool)kontroler.ViewData["ImaAktivnuSesiju"]!);
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public async Task Index_UserKojiNePostojiUBazi_BacaNullReferenceException()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, TestObjekti.Principal("nepostoji", "User"));

        await TestObjekti.OcekujIzuzetakAsync<NullReferenceException>(() => kontroler.Index());
    }

    //  ApproveUser 

    [Owner("Ajdin.L"), TestMethod]
    public async Task ApproveUser_NepostojeciKorisnik_VracaNotFoundBezSnimanja()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, TestObjekti.Principal("admin-1", "Administrator"));

        var rezultat = await kontroler.ApproveUser("nepostoji");

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task ApproveUser_KorisnikBezLoyalty_AktiviraClanarinuIKreiraProgramSaPopustom001()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("u1", clanarinaAktivna: false));
        var kontroler = Napravi(baza, TestObjekti.Principal("admin-1", "Administrator"));
        
        var rezultat = await kontroler.ApproveUser("u1");
        
        Provjere.Preusmjerenje(rezultat);
        using var provjera = baza.NoviKontekst();
        Assert.IsTrue((await provjera.Korisnik.SingleAsync()).ClanarinaAktivna);

        var program = await provjera.LoyaltyProgram.SingleAsync();
        Assert.AreEqual("u1", program.KorisnikId);
        Assert.AreEqual(0, program.UkupniBodovi);
        Assert.AreEqual(0.01, program.PopustPoBodu, 1e-9);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task ApproveUser_KorisnikVecImaLoyalty_NeKreiraNoviINeMijenjaPostojeci()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("u1", clanarinaAktivna: false),
            new LoyaltyProgram { KorisnikId = "u1", UkupniBodovi = 50, PopustPoBodu = 0.05 });
        var kontroler = Napravi(baza, TestObjekti.Principal("admin-1", "Administrator"));
        
        var rezultat = await kontroler.ApproveUser("u1");
       
        Provjere.Preusmjerenje(rezultat);
        using var provjera = baza.NoviKontekst();
        Assert.IsTrue((await provjera.Korisnik.SingleAsync()).ClanarinaAktivna);

        var program = await provjera.LoyaltyProgram.SingleAsync();
        Assert.AreEqual(50, program.UkupniBodovi);
        Assert.AreEqual(0.05, program.PopustPoBodu, 1e-9);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task ApproveUser_DrugiKorisniciOstajuNepromijenjeni()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("u1", clanarinaAktivna: false),
            TestObjekti.NoviKorisnik("u2", clanarinaAktivna: false));
        var kontroler = Napravi(baza, TestObjekti.Principal("admin-1", "Administrator"));

        await kontroler.ApproveUser("u1");

        using var provjera = baza.NoviKontekst();
        Assert.IsTrue((await provjera.Korisnik.SingleAsync(k => k.Id == "u1")).ClanarinaAktivna);
        Assert.IsFalse((await provjera.Korisnik.SingleAsync(k => k.Id == "u2")).ClanarinaAktivna);
        Assert.AreEqual(1, await provjera.LoyaltyProgram.CountAsync());
    }

    //  Privacy i Error 

    [Owner("Ajdin.L"), TestMethod]
    public void Privacy_VracaPrikaz()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza);

        Assert.IsInstanceOfType<ViewResult>(kontroler.Privacy());
    }

    [Owner("Ajdin.L"), TestMethod]
    public void Error_BezAktivneAktivnosti_KoristiTraceIdentifier()
    {
        var staraAktivnost = Activity.Current;
        try
        {
            Activity.Current = null;
            using var baza = new TestBaza();
            var kontroler = Napravi(baza, traceId: "trace-123");

            var model = Provjere.ModelPrikaza<ErrorViewModel>(kontroler.Error());

            Assert.AreEqual("trace-123", model.RequestId);
        }
        finally
        {
            Activity.Current = staraAktivnost;
        }
    }

    [Owner("Ajdin.L"), TestMethod]
    public void Error_SaAktivnomAktivnoscu_KoristiIdAktivnosti()
    {
        var staraAktivnost = Activity.Current;
        try
        {
            using var aktivnost = new Activity("test-operacija");
            aktivnost.Start();
            using var baza = new TestBaza();
            var kontroler = Napravi(baza, traceId: "trace-123");

            var model = Provjere.ModelPrikaza<ErrorViewModel>(kontroler.Error());

            Assert.AreEqual(aktivnost.Id, model.RequestId);
        }
        finally
        {
            Activity.Current = staraAktivnost;
        }
    }

    //  UcitajTakmicenja (PandaScore) 

    [Owner("Ajdin.L"), TestMethod]
    public async Task UcitajTakmicenja_NovaTakmicenja_DodajeIhPostavljaPorukuIPreusmjerava()
    {
        var handler = new LazniHttpHandler($"[{Json("Match A", "2030-05-20T10:00:00Z", "CS2")}," +
                                           $"{Json("Match B", "2030-06-01T10:00:00Z", "Dota 2")}]");
        using var baza = new TestBaza();
        var kontroler = NapraviZaUcitavanje(baza, handler);
       
        var rezultat = await kontroler.UcitajTakmicenja();
        
        Provjere.Preusmjerenje(rezultat);
        Assert.AreEqual(PorukaUspjeha, kontroler.TempData["Poruka"]);

        using var provjera = baza.NoviKontekst();
        var spremljena = await provjera.Takmicenje.OrderBy(t => t.Naziv).ToListAsync();
        Assert.AreEqual(2, spremljena.Count);
        Assert.AreEqual("Match A", spremljena[0].Naziv);
        Assert.AreEqual("CS2", spremljena[0].Igra);
        Assert.AreEqual(2030, spremljena[0].Datum.Year);
        Assert.AreEqual(5, spremljena[0].Datum.Month);
        Assert.AreEqual(20, spremljena[0].Datum.Day);
        Assert.AreEqual("Match B", spremljena[1].Naziv);
        Assert.AreEqual("Dota 2", spremljena[1].Igra);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task UcitajTakmicenja_SaljeZahtjevNaAdresuIzKonfiguracijeSaBearerTokenom()
    {
        var handler = new LazniHttpHandler("[]");
        using var baza = new TestBaza();
        var kontroler = NapraviZaUcitavanje(baza, handler);

        await kontroler.UcitajTakmicenja();

        Assert.AreEqual(1, handler.BrojZahtjeva);
        Assert.AreEqual("https://api.test.local/matches", handler.Adresa!.ToString());
        Assert.AreEqual("Bearer tajni-token", handler.Autorizacija);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task UcitajTakmicenja_PrazanOdgovor_NistaNeDodajeAliPostavljaPoruku()
    {
        using var baza = new TestBaza();
        var kontroler = NapraviZaUcitavanje(baza, new LazniHttpHandler("[]"));

        var rezultat = await kontroler.UcitajTakmicenja();

        Provjere.Preusmjerenje(rezultat);
        Assert.AreEqual(PorukaUspjeha, kontroler.TempData["Poruka"]);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.Takmicenje.CountAsync());
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task UcitajTakmicenja_PostojeciNazivIDatum_NeDuplira()
    {
        // Arrange: isti naziv i isti dan vec postoje u bazi
        using var baza = new TestBaza();
        baza.Dodaj(new Takmicenje { Naziv = "Match A", Igra = "CS2", Datum = new DateTime(2030, 5, 20, 11, 0, 0) });
        var handler = new LazniHttpHandler($"[{Json("Match A", "2030-05-20T10:00:00Z")}," +
                                           $"{Json("Match B", "2030-05-20T10:00:00Z")}]");
        var kontroler = NapraviZaUcitavanje(baza, handler);
       
        await kontroler.UcitajTakmicenja();
      
        using var provjera = baza.NoviKontekst();
        var nazivi = await provjera.Takmicenje.Select(t => t.Naziv).OrderBy(n => n).ToListAsync();
        CollectionAssert.AreEqual(new[] { "Match A", "Match B" }, nazivi);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task UcitajTakmicenja_ProslaTakmicenjaIzBazeSeBrisu()
    {
        using var baza = new TestBaza();
        baza.Dodaj(
            new Takmicenje { Naziv = "Prosli", Igra = "CS2", Datum = DateTime.Today.AddDays(-3) },
            new Takmicenje { Naziv = "Buduci", Igra = "CS2", Datum = DateTime.Today.AddDays(3) });
        var kontroler = NapraviZaUcitavanje(baza, new LazniHttpHandler("[]"));

        await kontroler.UcitajTakmicenja();

        using var provjera = baza.NoviKontekst();
        var nazivi = await provjera.Takmicenje.Select(t => t.Naziv).ToListAsync();
        CollectionAssert.AreEqual(new[] { "Buduci" }, nazivi);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task UcitajTakmicenja_NedostajuciPodaci_KoristiPodrazumijevaneVrijednosti()
    {
        // Arrange: bez naziva, bez datuma i bez igre
        var handler = new LazniHttpHandler("[{\"name\":null,\"begin_at\":null,\"videogame\":null}]");
        using var baza = new TestBaza();
        var kontroler = NapraviZaUcitavanje(baza, handler);
       
        await kontroler.UcitajTakmicenja();
        
        using var provjera = baza.NoviKontekst();
        var takmicenje = await provjera.Takmicenje.SingleAsync();
        Assert.AreEqual("Esports Match", takmicenje.Naziv);
        Assert.AreEqual("Game", takmicenje.Igra);
        Assert.IsTrue(Math.Abs((takmicenje.Datum - DateTime.Now).TotalMinutes) < 5);
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public async Task UcitajTakmicenja_NeispravanDatum_PostajeMinimalniDatum()
    {
        // DateTime.TryParse pri neuspjehu postavlja out parametar na DateTime.MinValue,
        // pa se prethodno postavljeni DateTime.Now gubi. Takvo takmicenje je odmah "proslo"
        // i bit ce obrisano pri sljedecem ucitavanju Index stranice.
        var handler = new LazniHttpHandler($"[{Json("Match A", "nije-datum")}]");
        using var baza = new TestBaza();
        var kontroler = NapraviZaUcitavanje(baza, handler);

        await kontroler.UcitajTakmicenja();

        using var provjera = baza.NoviKontekst();
        var takmicenje = await provjera.Takmicenje.SingleAsync();
        Assert.AreEqual(DateTime.MinValue, takmicenje.Datum);
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public async Task UcitajTakmicenja_IstaTakmicenjaUIstomOdgovoru_SeDuplirajuJerSeSnimajuNaKraju()
    {
        // Provjera duplikata radi nad bazom, a nova takmicenja se snimaju tek nakon petlje.
        var stavka = Json("Match A", "2030-05-20T10:00:00Z");
        var handler = new LazniHttpHandler($"[{stavka},{stavka}]");
        using var baza = new TestBaza();
        var kontroler = NapraviZaUcitavanje(baza, handler);

        await kontroler.UcitajTakmicenja();

        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(2, await provjera.Takmicenje.CountAsync());
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public async Task UcitajTakmicenja_GreskaVanjskogServisa_BacaIzuzetakAliProslaTakmicenjaSuVecObrisana()
    {
        using var baza = new TestBaza();
        baza.Dodaj(new Takmicenje { Naziv = "Prosli", Igra = "CS2", Datum = DateTime.Today.AddDays(-3) });
        var handler = new LazniHttpHandler("greska", HttpStatusCode.InternalServerError);
        var kontroler = NapraviZaUcitavanje(baza, handler);
       
        await TestObjekti.OcekujIzuzetakAsync<HttpRequestException>(() => kontroler.UcitajTakmicenja());
        
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.Takmicenje.CountAsync());
    }

    //  Autorizacija i atributi 

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(nameof(HomeController.Index))]
    [DataRow(nameof(HomeController.Privacy))]
    public void JavneAkcije_ImajuAllowAnonymous(string akcija)
    {
        var metoda = typeof(HomeController).GetMethod(akcija)!;

        Assert.IsNotNull(metoda.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(nameof(HomeController.UcitajTakmicenja))]
    [DataRow(nameof(HomeController.ApproveUser))]
    public void AdminAkcije_ZahtijevajuUloguAdministrator(string akcija)
    {
        var atributi = typeof(HomeController).GetMethod(akcija)!.GetCustomAttributes<AuthorizeAttribute>().ToList();

        Assert.IsTrue(atributi.Count > 0);
        Assert.IsTrue(atributi.All(a => a.Roles == "Administrator"));
    }

    [Owner("Ajdin.L"), TestMethod]
    public void ApproveUser_JeSamoHttpPost()
    {
        var metoda = typeof(HomeController).GetMethod(nameof(HomeController.ApproveUser))!;

        Assert.IsNotNull(metoda.GetCustomAttribute<HttpPostAttribute>());
    }

    [Owner("Ajdin.L"), TestMethod]
    public void Error_IskljucujeKesiranje()
    {
        var atribut = typeof(HomeController).GetMethod(nameof(HomeController.Error))!
            .GetCustomAttribute<ResponseCacheAttribute>();

        Assert.IsNotNull(atribut);
        Assert.IsTrue(atribut.NoStore);
        Assert.AreEqual(0, atribut.Duration);
    }
}