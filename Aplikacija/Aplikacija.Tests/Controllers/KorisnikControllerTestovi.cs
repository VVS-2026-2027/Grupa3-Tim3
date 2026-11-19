using System.Reflection;
using Aplikacija.Controllers;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aplikacija.Tests.Controllers;

[TestClass]
public class KorisnikControllerTests
{
    private static KorisnikController Napravi(TestBaza baza, params string[] uloge)
    {
        var kontroler = new KorisnikController(baza.Context, TestObjekti.NapraviUserManager(baza.Context))
        {
            ControllerContext = TestObjekti.KontekstKontrolera(TestObjekti.Principal("admin-1", uloge))
        };
        return kontroler;
    }

    //  Pocetna 

    [Owner("Ajdin.L"), TestMethod]
    [DataRow("Administrator", "AdminPanel")]
    [DataRow("Employee", "EmployeePanel")]
    [DataRow("User", "UserPanel")]
    public void Pocetna_PrikazOvisiOUlozi(string uloga, string ocekivaniPrikaz)
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, uloga);

        var rezultat = kontroler.Pocetna();

        Assert.AreEqual(ocekivaniPrikaz, Provjere.Prikaz(rezultat).ViewName);
    }

    [Owner("Ajdin.L"), TestMethod]
    public void Pocetna_BezPoznateUloge_VracaUserPanel()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza);

        Assert.AreEqual("UserPanel", Provjere.Prikaz(kontroler.Pocetna()).ViewName);
    }

    [Owner("Ajdin.L"), TestMethod]
    public void Pocetna_AdministratorIEmployee_AdministratorImaPrednost()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator", "Employee");

        Assert.AreEqual("AdminPanel", Provjere.Prikaz(kontroler.Pocetna()).ViewName);
    }

    //  Index 

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_BezKorisnika_VracaPrazneListe()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.Index();

        Assert.AreEqual(0, Provjere.ModelPrikaza<List<Korisnik>>(rezultat).Count);
        var programi = kontroler.ViewData["LoyaltyPrograms"];
        Assert.IsInstanceOfType<List<LoyaltyProgram>>(programi);
        Assert.AreEqual(0, ((List<LoyaltyProgram>)programi!).Count);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Index_SaKorisnicima_VracaSveKorisnikeISveLoyaltyPrograme()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"), TestObjekti.NoviKorisnik("k2"),
            new LoyaltyProgram { KorisnikId = "k1", UkupniBodovi = 40, PopustPoBodu = 0.04 });
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.Index();
        
        var korisnici = Provjere.ModelPrikaza<List<Korisnik>>(rezultat);
        CollectionAssert.AreEquivalent(new[] { "k1", "k2" }, korisnici.Select(k => k.Id).ToArray());

        var programi = (List<LoyaltyProgram>)kontroler.ViewData["LoyaltyPrograms"]!;
        Assert.AreEqual(1, programi.Count);
        Assert.AreEqual(40, programi[0].UkupniBodovi);
    }

    //  Details 

    [Owner("Ajdin.L"), TestMethod]
    public async Task Details_NullId_VracaNotFound()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator");

        Assert.IsInstanceOfType<NotFoundResult>(await kontroler.Details(null!));
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Details_NepostojeciKorisnik_VracaNotFound()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator");

        Assert.IsInstanceOfType<NotFoundResult>(await kontroler.Details("nepostoji"));
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Details_KorisnikBezLoyaltyPrograma_PrikazujeNulaBodova()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"));
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.Details("k1");

        Assert.AreEqual("k1", Provjere.ModelPrikaza<Korisnik>(rezultat).Id);
        Assert.AreEqual(0, kontroler.ViewData["LoyaltyPoints"]);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task Details_KorisnikSaLoyaltyProgramom_PrikazujeBodove()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"), TestObjekti.NoviKorisnik("k2"),
            new LoyaltyProgram { KorisnikId = "k1", UkupniBodovi = 75, PopustPoBodu = 0.04 },
            new LoyaltyProgram { KorisnikId = "k2", UkupniBodovi = 999, PopustPoBodu = 0.04 });
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.Details("k1");

        Assert.AreEqual("k1", Provjere.ModelPrikaza<Korisnik>(rezultat).Id);
        Assert.AreEqual(75, kontroler.ViewData["LoyaltyPoints"]);
    }

    //  Create 

    [Owner("Ajdin.L"), TestMethod]
    public void CreateGet_VracaPrikaz()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator");

        Assert.IsInstanceOfType<ViewResult>(kontroler.Create());
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task CreatePost_ValidniPodaci_KreiraKorisnikaISvrstavaGaUUloguUser()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uloga("User"));
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.Create("Amar", "Hodzic", "amar@test.ba", "Test123!", true);
        
        Provjere.Preusmjerenje(rezultat);
        Assert.IsTrue(kontroler.ModelState.IsValid);

        using var provjera = baza.NoviKontekst();
        var korisnik = await provjera.Korisnik.SingleAsync();
        Assert.AreEqual("Amar", korisnik.Ime);
        Assert.AreEqual("Hodzic", korisnik.Prezime);
        Assert.AreEqual("amar@test.ba", korisnik.Email);
        Assert.AreEqual("amar@test.ba", korisnik.UserName);
        Assert.IsTrue(korisnik.ClanarinaAktivna);
        Assert.IsFalse(string.IsNullOrEmpty(korisnik.PasswordHash));
        Assert.AreNotEqual("Test123!", korisnik.PasswordHash);

        var dodjela = await provjera.UserRoles.SingleAsync();
        Assert.AreEqual(korisnik.Id, dodjela.UserId);
        Assert.AreEqual("uloga-user", dodjela.RoleId);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task CreatePost_SlabaLozinka_VracaPrikazSaGreskamaINeKreiraKorisnika()
    {
        
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uloga("User"));
        var kontroler = Napravi(baza, "Administrator");
        
        var rezultat = await kontroler.Create("Amar", "Hodzic", "amar@test.ba", "abc", false);
        
        var model = Provjere.ModelPrikaza<Korisnik>(rezultat);
        Assert.AreEqual("amar@test.ba", model.Email);
        Assert.IsFalse(kontroler.ModelState.IsValid);
        Assert.IsTrue(kontroler.ModelState.ErrorCount > 0);

        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.Korisnik.CountAsync());
        Assert.AreEqual(0, await provjera.UserRoles.CountAsync());
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task CreatePost_PostojeciEmail_VracaPrikazSaGreskomINeDuplira()
    {
        
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uloga("User"), TestObjekti.NoviKorisnik("amar"));
        var kontroler = Napravi(baza, "Administrator");
        
        var rezultat = await kontroler.Create("Drugi", "Korisnik", "amar@test.ba", "Test123!", false);
        
        Provjere.ModelPrikaza<Korisnik>(rezultat);
        Assert.IsFalse(kontroler.ModelState.IsValid);

        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(1, await provjera.Korisnik.CountAsync());
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public async Task CreatePost_UlogaUserNePostoji_BacaIzuzetakAliKorisnikJeVecKreiran()
    {
        // Arrange: uloga "User" nije u bazi
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator");
        
        await TestObjekti.OcekujIzuzetakAsync<InvalidOperationException>(() =>
            kontroler.Create("Amar", "Hodzic", "amar@test.ba", "Test123!", false));
        // Assert: korisnik je kreiran prije dodjele uloge i ostaje bez uloge
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(1, await provjera.Korisnik.CountAsync());
        Assert.AreEqual(0, await provjera.UserRoles.CountAsync());
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public async Task CreatePost_NeispravnoImeIPrezime_KontrolerIhIpakPrihvata()
    {
        // Create prima obicne parametre, pa atributi iz Korisnik modela (regex, duzina) ne vrijede.
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uloga("User"));
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.Create("123", "!!", "x@test.ba", "Test123!", false);

        Provjere.Preusmjerenje(rezultat);
        using var provjera = baza.NoviKontekst();
        var korisnik = await provjera.Korisnik.SingleAsync();
        Assert.AreEqual("123", korisnik.Ime);
        Assert.AreEqual("!!", korisnik.Prezime);
    }

    //  Edit 

    [Owner("Ajdin.L"), TestMethod]
    public async Task EditGet_NullId_VracaNotFound()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator");

        Assert.IsInstanceOfType<NotFoundResult>(await kontroler.Edit((string)null!));
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task EditGet_NepostojeciKorisnik_VracaNotFound()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator");

        Assert.IsInstanceOfType<NotFoundResult>(await kontroler.Edit("nepostoji"));
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task EditGet_PostojeciKorisnik_VracaPrikazSaModelom()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1", "Amar"));
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.Edit("k1");

        var model = Provjere.ModelPrikaza<Korisnik>(rezultat);
        Assert.AreEqual("k1", model.Id);
        Assert.AreEqual("Amar", model.Ime);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task EditPost_NepostojeciKorisnik_VracaNotFoundBezSnimanja()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.Edit("nepostoji", "A", "B", "a@b.ba", true);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task EditPost_PostojeciKorisnik_MijenjaPodatkeIPreusmjerava()
    {
       
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1", "Amar", "Hodzic", clanarinaAktivna: false));
        var kontroler = Napravi(baza, "Administrator");
       
        var rezultat = await kontroler.Edit("k1", "Emir", "Kovac", "emir@test.ba", true);
        
        Provjere.Preusmjerenje(rezultat);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);

        using var provjera = baza.NoviKontekst();
        var korisnik = await provjera.Korisnik.SingleAsync();
        Assert.AreEqual("Emir", korisnik.Ime);
        Assert.AreEqual("Kovac", korisnik.Prezime);
        Assert.AreEqual("emir@test.ba", korisnik.Email);
        Assert.AreEqual("emir@test.ba", korisnik.UserName);
        Assert.IsTrue(korisnik.ClanarinaAktivna);
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public async Task EditPost_PromjenaEmaila_NormalizovaniNaziviOstajuStari()
    {
        // Edit ne koristi UserManager, pa NormalizedUserName/NormalizedEmail ostaju stari.
        // Identity trazi korisnika po normalizovanom imenu, pa prijava s novim emailom ne bi radila.
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"));
        var kontroler = Napravi(baza, "Administrator");

        await kontroler.Edit("k1", "Amar", "Hodzic", "novi@test.ba", false);

        using var provjera = baza.NoviKontekst();
        var korisnik = await provjera.Korisnik.SingleAsync();
        Assert.AreEqual("novi@test.ba", korisnik.UserName);
        Assert.AreEqual("K1@TEST.BA", korisnik.NormalizedUserName);
        Assert.AreEqual("K1@TEST.BA", korisnik.NormalizedEmail);
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public async Task EditPost_NeispravanModelState_KontrolerIpakSnimaPromjene()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1", "Amar"));
        var kontroler = Napravi(baza, "Administrator");
        kontroler.ModelState.AddModelError("Ime", "Neispravno ime.");

        var rezultat = await kontroler.Edit("k1", "123", "Hodzic", "k1@test.ba", false);

        Provjere.Preusmjerenje(rezultat);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual("123", (await provjera.Korisnik.SingleAsync()).Ime);
        Assert.IsFalse(kontroler.ModelState.IsValid);
    }

    //  Delete 

    [Owner("Ajdin.L"), TestMethod]
    public async Task DeleteGet_NullId_VracaNotFound()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator");

        Assert.IsInstanceOfType<NotFoundResult>(await kontroler.Delete(null!));
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task DeleteGet_NepostojeciKorisnik_VracaNotFound()
    {
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator");

        Assert.IsInstanceOfType<NotFoundResult>(await kontroler.Delete("nepostoji"));
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task DeleteGet_PostojeciKorisnik_VracaPrikazSaModelom()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"));
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.Delete("k1");

        Assert.AreEqual("k1", Provjere.ModelPrikaza<Korisnik>(rezultat).Id);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task DeleteConfirmed_PostojeciKorisnik_BriseSamoTogKorisnika()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"), TestObjekti.NoviKorisnik("k2"));
        var kontroler = Napravi(baza, "Administrator");
       
        var rezultat = await kontroler.DeleteConfirmed("k1");
        
        Provjere.Preusmjerenje(rezultat);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);

        using var provjera = baza.NoviKontekst();
        var preostali = await provjera.Korisnik.SingleAsync();
        Assert.AreEqual("k2", preostali.Id);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task DeleteConfirmed_NepostojeciKorisnik_PreusmjeravaBezPromjenaUBazi()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"));
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.DeleteConfirmed("nepostoji");

        Provjere.Preusmjerenje(rezultat);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(1, await provjera.Korisnik.CountAsync());
    }

    //  AddLoyaltyPoints 

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(0), DataRow(-1), DataRow(-100)]
    public async Task AddLoyaltyPoints_BodoviNisuPozitivni_PreusmjeravaBezSnimanja(int bodovi)
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"));
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.AddLoyaltyPoints("k1", bodovi);

        Provjere.Preusmjerenje(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.LoyaltyProgram.CountAsync());
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task AddLoyaltyPoints_KorisnikNemaProgram_KreiraProgramSaPopustom004()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"));
        var kontroler = Napravi(baza, "Administrator");
        
        var rezultat = await kontroler.AddLoyaltyPoints("k1", 30);
       
        Provjere.Preusmjerenje(rezultat);
        using var provjera = baza.NoviKontekst();
        var program = await provjera.LoyaltyProgram.SingleAsync();
        Assert.AreEqual("k1", program.KorisnikId);
        Assert.AreEqual(30, program.UkupniBodovi);
        Assert.AreEqual(0.04, program.PopustPoBodu, 1e-9);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task AddLoyaltyPoints_KorisnikImaProgram_UvecavaBodoveBezPromjenePopusta()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"),
            new LoyaltyProgram { KorisnikId = "k1", UkupniBodovi = 40, PopustPoBodu = 0.05 });
        var kontroler = Napravi(baza, "Administrator");
    
        var rezultat = await kontroler.AddLoyaltyPoints("k1", 25);
       
        Provjere.Preusmjerenje(rezultat);
        using var provjera = baza.NoviKontekst();
        var program = await provjera.LoyaltyProgram.SingleAsync();
        Assert.AreEqual(65, program.UkupniBodovi);
        Assert.AreEqual(0.05, program.PopustPoBodu, 1e-9);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task AddLoyaltyPoints_ViseDodavanja_BodoviSeSabiraju()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"));
        var kontroler = Napravi(baza, "Administrator");

        await kontroler.AddLoyaltyPoints("k1", 10);
        await kontroler.AddLoyaltyPoints("k1", 15);
        await kontroler.AddLoyaltyPoints("k1", 5);

        using var provjera = baza.NoviKontekst();
        var program = await provjera.LoyaltyProgram.SingleAsync();
        Assert.AreEqual(30, program.UkupniBodovi);
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public async Task AddLoyaltyPoints_NepostojeciKorisnik_BacaDbUpdateException()
    {
        // Kontroler ne provjerava postoji li korisnik; ogranicenje stranog kljuca baca izuzetak.
        using var baza = new TestBaza();
        var kontroler = Napravi(baza, "Administrator");

        await TestObjekti.OcekujIzuzetakAsync<DbUpdateException>(() =>
            kontroler.AddLoyaltyPoints("nepostoji", 10));
    }

    //  RemoveLoyaltyPoints 

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(0), DataRow(-1), DataRow(-100)]
    public async Task RemoveLoyaltyPoints_BodoviNisuPozitivni_PreusmjeravaBezSnimanja(int bodovi)
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"),
            new LoyaltyProgram { KorisnikId = "k1", UkupniBodovi = 40, PopustPoBodu = 0.04 });
        var kontroler = Napravi(baza, "Employee");

        var rezultat = await kontroler.RemoveLoyaltyPoints("k1", bodovi);

        Provjere.Preusmjerenje(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(40, (await provjera.LoyaltyProgram.SingleAsync()).UkupniBodovi);
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(15, 25)]
    [DataRow(40, 0)]
    [DataRow(41, 0)]
    [DataRow(1000, 0)]
    public async Task RemoveLoyaltyPoints_OduzimaBodoveINikadaNeIdeIspodNule(int oduzmi, int ocekivano)
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"),
            new LoyaltyProgram { KorisnikId = "k1", UkupniBodovi = 40, PopustPoBodu = 0.04 });
        var kontroler = Napravi(baza, "Administrator");
      
        var rezultat = await kontroler.RemoveLoyaltyPoints("k1", oduzmi);
      
        Provjere.Preusmjerenje(rezultat);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(ocekivano, (await provjera.LoyaltyProgram.SingleAsync()).UkupniBodovi);
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task RemoveLoyaltyPoints_KorisnikNemaProgram_PreusmjeravaINeKreiraProgram()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"));
        var kontroler = Napravi(baza, "Administrator");

        var rezultat = await kontroler.RemoveLoyaltyPoints("k1", 10);

        Provjere.Preusmjerenje(rezultat);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.LoyaltyProgram.CountAsync());
    }

    [Owner("Ajdin.L"), TestMethod]
    public async Task RemoveLoyaltyPoints_NeDiraProgrameDrugihKorisnika()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"), TestObjekti.NoviKorisnik("k2"),
            new LoyaltyProgram { KorisnikId = "k1", UkupniBodovi = 40, PopustPoBodu = 0.04 },
            new LoyaltyProgram { KorisnikId = "k2", UkupniBodovi = 70, PopustPoBodu = 0.04 });
        var kontroler = Napravi(baza, "Administrator");

        await kontroler.RemoveLoyaltyPoints("k1", 10);

        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(30, (await provjera.LoyaltyProgram.SingleAsync(l => l.KorisnikId == "k1")).UkupniBodovi);
        Assert.AreEqual(70, (await provjera.LoyaltyProgram.SingleAsync(l => l.KorisnikId == "k2")).UkupniBodovi);
    }

    //  Autorizacija (atributi) 

    [Owner("Ajdin.L"), TestMethod]
    public void Kontroler_ImaAuthorizeNaNivouKlase()
    {
        Assert.IsNotNull(typeof(KorisnikController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(nameof(KorisnikController.Create), "Administrator")]
    [DataRow(nameof(KorisnikController.Edit), "Administrator")]
    [DataRow(nameof(KorisnikController.Delete), "Administrator")]
    [DataRow(nameof(KorisnikController.DeleteConfirmed), "Administrator")]
    [DataRow(nameof(KorisnikController.AddLoyaltyPoints), "Administrator")]
    [DataRow(nameof(KorisnikController.RemoveLoyaltyPoints), "Administrator,Employee")]
    public void Akcija_JeOgranicenaNaOcekivaneUloge(string akcija, string ocekivaneUloge)
    {
        var metode = typeof(KorisnikController).GetMethods().Where(m => m.Name == akcija).ToList();

        Assert.IsTrue(metode.Count > 0);
        foreach (var metoda in metode)
            Assert.AreEqual(ocekivaneUloge, metoda.GetCustomAttribute<AuthorizeAttribute>()?.Roles,
                $"{akcija} ({metoda.GetParameters().Length} parametara)");
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(nameof(KorisnikController.DeleteConfirmed))]
    [DataRow(nameof(KorisnikController.AddLoyaltyPoints))]
    [DataRow(nameof(KorisnikController.RemoveLoyaltyPoints))]
    public void AkcijaKojaMijenjaPodatke_ImaHttpPostIAntiForgeryToken(string akcija)
    {
        var metoda = typeof(KorisnikController).GetMethod(akcija)!;

        Assert.IsNotNull(metoda.GetCustomAttribute<HttpPostAttribute>());
        Assert.IsNotNull(metoda.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }
}