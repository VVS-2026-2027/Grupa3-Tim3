using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Cvjecara_Latica.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Cvjecara_Latica.Tests.ErnadTest;

/// <summary>
/// Unit testovi za UnclaimedOrderChecker (BackgroundService) - Clan 3.
/// ZAMJENSKI OBJEKTI: Mock&lt;IEmailService&gt; (+ Verify poslanih podsjetnika i obavijesti o deaktivaciji)
/// i InMemory baza umjesto prave baze.
/// DATA-DRIVEN: TestData/Ernad/podsjetnici.xml.
/// Servis se pokrece jednom (StartAsync + StopAsync): StopAsync ceka da se zavrsi prva provjera,
/// pa nema cekanja od 24 sata niti nasumicnih pauza.
/// </summary>
[TestClass]
[TestCategory("UnclaimedOrderChecker")]
public class UnclaimedOrderCheckerTests
{
    private const string Adresa = "k1@test.com";

    private string _imeBaze = null!;
    private Mock<IEmailService> _emailMock = null!;
    private ServiceProvider _servisi = null!;

    [TestInitialize]
    public void Postavi()
    {
        _imeBaze = Guid.NewGuid().ToString();
        _emailMock = new Mock<IEmailService>();

        var kolekcija = new ServiceCollection();
        kolekcija.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(_imeBaze));
        kolekcija.AddSingleton(_emailMock.Object);
        _servisi = kolekcija.BuildServiceProvider();
    }

    [TestCleanup]
    public void Ocisti() => _servisi.Dispose();

    // ---------- pomocne metode ----------

    private ApplicationDbContext NovaBaza()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(_imeBaze).Options);

    private void Dodaj(params object[] entiteti)
    {
        using var baza = NovaBaza();
        baza.AddRange(entiteti);
        baza.SaveChanges();
    }

    private static Order Narudzba(int id, double satiKasnjenja, bool preuzeta = false, string osobaId = ErnadPomocnici.KorisnikId)
    {
        var narudzba = ErnadPomocnici.NovaNarudzba(id, osobaId);
        narudzba.IsPickedUp = preuzeta;
        narudzba.DeliveryDate = DateTime.Now.AddHours(-satiKasnjenja);
        return narudzba;
    }

    private async Task PokreniProvjeruJednomAsync(CancellationToken token = default)
    {
        using var provjera = new UnclaimedOrderChecker(_servisi);
        await provjera.StartAsync(token);
        await provjera.StopAsync(CancellationToken.None);
    }

    private Person UcitajKorisnika(string id)
    {
        using var baza = NovaBaza();
        return baza.Users.Single(u => u.Id == id);
    }

    private void ProvjeriBrojObavijesti(int podsjetnici, int deaktivacije)
    {
        _emailMock.Verify(e => e.SendReminder(It.IsAny<string>()), Times.Exactly(podsjetnici));
        _emailMock.Verify(e => e.SendDeactivationNotice(It.IsAny<string>()), Times.Exactly(deaktivacije));
    }

    // ---------- podsjetnici ----------

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_PraznaBaza_NeSaljeNiJedanEmail()
    {
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        ProvjeriBrojObavijesti(0, 0);
    }

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_NarudzbaKasniManjeOd24Sata_SaljePodsjetnikKupcu()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba(), Narudzba(1, 12));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        _emailMock.Verify(e => e.SendReminder(Adresa), Times.Once());
    }

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_NarudzbaKasniManjeOd24Sata_NeSaljeObavijestODeaktivaciji()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba(), Narudzba(1, 12));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        _emailMock.Verify(e => e.SendDeactivationNotice(It.IsAny<string>()), Times.Never());
    }

    // ---------- deaktivacija ----------

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_NarudzbaKasniViseOd24Sata_SaljeObavijestODeaktivaciji()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba(), Narudzba(1, 48));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        _emailMock.Verify(e => e.SendDeactivationNotice(Adresa), Times.Once());
    }

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_NarudzbaKasniViseOd24Sata_ZakljucavaKorisnickiRacun()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba(), Narudzba(1, 48));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        var korisnik = UcitajKorisnika(ErnadPomocnici.KorisnikId);
        Assert.IsTrue(korisnik.LockoutEnabled);
        Assert.AreEqual(DateTimeOffset.MaxValue, korisnik.LockoutEnd);
    }

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_DvijeKasneNarudzbeIstogKorisnika_DeaktivacijaSeSaljeSamoJednom()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba(), Narudzba(1, 48), Narudzba(2, 72));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        _emailMock.Verify(e => e.SendDeactivationNotice(Adresa), Times.Once());
    }

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_DvijeKasneNarudzbeIstogKorisnika_PodsjetnikSeSaljeZaSvakuNarudzbu()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba(), Narudzba(1, 48), Narudzba(2, 72));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        _emailMock.Verify(e => e.SendReminder(Adresa), Times.Exactly(2));
    }

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_DvaKorisnikaRazlicitoKasne_DeaktiviraSamoOnogKojiKasniViseOd24Sata()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba("k1"), ErnadPomocnici.NovaOsoba("k2"),
            Narudzba(1, 12, osobaId: "k1"), Narudzba(2, 48, osobaId: "k2"));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        _emailMock.Verify(e => e.SendDeactivationNotice("k2@test.com"), Times.Once());
        _emailMock.Verify(e => e.SendDeactivationNotice("k1@test.com"), Times.Never());
    }

    // ---------- narudzbe koje se ne obradjuju ----------

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_PreuzetaNarudzba_NeSaljeNiJedanEmail()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba(), Narudzba(1, 72, preuzeta: true));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        ProvjeriBrojObavijesti(0, 0);
    }

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_NarudzbaJosNijeDospjela_NeSaljeNiJedanEmail()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba(), Narudzba(1, -48));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        ProvjeriBrojObavijesti(0, 0);
    }

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_NarudzbaBezKorisnikaUBazi_NeSaljeNiJedanEmail()
    {
        // Arrange
        Dodaj(Narudzba(1, 72, osobaId: "nepoznat"));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        ProvjeriBrojObavijesti(0, 0);
    }

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_ZaustavljanjePrijePocetka_NistaSeNeProvjerava()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba(), Narudzba(1, 72));
        using var otkazano = new CancellationTokenSource();
        await otkazano.CancelAsync();
        // Act
        await PokreniProvjeruJednomAsync(otkazano.Token);
        // Assert
        ProvjeriBrojObavijesti(0, 0);
    }

    // ---------- izuzeci ----------

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_EmailServisBacaIzuzetak_ServisSeNeUrusava()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba(), Narudzba(1, 12));
        _emailMock.Setup(e => e.SendReminder(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP nije dostupan"));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        _emailMock.Verify(e => e.SendReminder(Adresa), Times.Once());
    }

    [Owner("Ernad"), TestMethod]
    public async Task Provjera_EmailServisBacaIzuzetak_KorisnikSeNeDeaktivira()
    {
        // Arrange
        Dodaj(ErnadPomocnici.NovaOsoba(), Narudzba(1, 48));
        _emailMock.Setup(e => e.SendReminder(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP nije dostupan"));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        Assert.IsFalse(UcitajKorisnika(ErnadPomocnici.KorisnikId).LockoutEnabled);
    }

    // ---------- postojece ponasanje (moguci defekt) ----------

    [Owner("Ernad"), TestMethod]
    [TestCategory("PostojecePonasanje")]
    [Description("Identity za nove korisnike postavlja LockoutEnabled = true, pa se takav korisnik nikad ne deaktivira.")]
    public async Task Provjera_KorisnikSaLockoutEnabledTrue_NijeDeaktiviranIakoKasni()
    {
        // Arrange
        var osoba = ErnadPomocnici.NovaOsoba();
        osoba.LockoutEnabled = true;
        Dodaj(osoba, Narudzba(1, 72));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        _emailMock.Verify(e => e.SendDeactivationNotice(It.IsAny<string>()), Times.Never());
    }

    // ---------- DATA-DRIVEN (XML) ----------

    [Owner("Ernad"), TestMethod]
    [DynamicData(nameof(ErnadPomocnici.PodsjetniciSlucajevi), typeof(ErnadPomocnici), DynamicDataSourceType.Method)]
    public async Task Provjera_RazliciteNarudzbe_SaljeOcekivaniBrojObavijesti(
        string naziv, double satiKasnjenja, bool preuzeta, bool lockoutOmogucen,
        int ocekivaniPodsjetnici, int ocekivaneDeaktivacije)
    {
        // Arrange
        var osoba = ErnadPomocnici.NovaOsoba();
        osoba.LockoutEnabled = lockoutOmogucen;
        Dodaj(osoba, Narudzba(1, satiKasnjenja, preuzeta));
        // Act
        await PokreniProvjeruJednomAsync();
        // Assert
        _emailMock.Verify(e => e.SendReminder(Adresa), Times.Exactly(ocekivaniPodsjetnici), naziv);
        _emailMock.Verify(e => e.SendDeactivationNotice(Adresa), Times.Exactly(ocekivaneDeaktivacije), naziv);
    }
}
