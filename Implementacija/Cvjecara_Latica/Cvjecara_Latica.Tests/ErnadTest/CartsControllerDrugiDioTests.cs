using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Cvjecara_Latica.Services;
using Cvjecara_Latica.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cvjecara_Latica.Tests.ErnadTest;

/// <summary>
/// Unit testovi za CartsController - II dio (Clan 3):
/// ConfirmOrder, SubmitOrderFinal, SubmitRating, FinishRating.
/// ZAMJENSKI OBJEKTI: Mock&lt;IEmailService&gt; (+ Verify poslanih mailova), InMemory baza umjesto prave baze,
/// Mock&lt;ITempDataProvider&gt; za TempData.
/// DATA-DRIVEN (XML): korpa_ukupno.xml, nacini_placanja.xml, ocjene.xml.
/// </summary>
[TestClass]
[TestCategory("CartsController-II")]
public class CartsControllerDrugiDioTests
{
    private const string KorisnikId = ErnadPomocnici.KorisnikId;
    private const string EmailKorisnika = "k1@test.com";
    private const string BrojKartice = "4111111111111111";
    private static readonly DateTime DatumIsporuke = new(2026, 12, 1, 10, 0, 0);

    private ApplicationDbContext _context = null!;
    private Mock<IEmailService> _emailMock = null!;
    private CartsController _kontroler = null!;

    [TestInitialize]
    public void Postavi()
    {
        _context = TestDbContextFactory.Kreiraj();
        _emailMock = new Mock<IEmailService>();
        _kontroler = new CartsController(_context, _emailMock.Object);
        ErnadPomocnici.PostaviKorisnika(_kontroler, KorisnikId);
        _kontroler.TempData = ErnadPomocnici.NoviTempData();
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontroler.Dispose();
        _context.Dispose();
    }

    // ---------- pomocne metode ----------

    /// <summary>Korisnik k1 sa jednim proizvodom (Ruza, 15) u korpi.</summary>
    private void PripremiKorpu(int? kolicina = 2)
        => ErnadPomocnici.Snimi(_context, ErnadPomocnici.NovaOsoba(), ErnadPomocnici.NoviProizvod(),
            ErnadPomocnici.NovaStavka(1, 1, KorisnikId, kolicina));

    private Task<IActionResult> Potvrdi(string nacinPlacanja = "Delivery", string? kartica = null)
        => _kontroler.SubmitOrderFinal("Ime Prezime", "Sarajevo", "Zmaja od Bosne 1", "061111222",
            nacinPlacanja, 30.0, kartica, DatumIsporuke);

    private void PostaviEmailKojiBacaIzuzetak()
        => _emailMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP nije dostupan"));

    // ==================== ConfirmOrder ====================

    [Owner("Ernad"), TestMethod]
    public void ConfirmOrder_PrijavljenKorisnik_VracaPogledConfirmOrder()
    {
        // Act
        var rezultat = _kontroler.ConfirmOrder("Ime", "Grad", "Adresa 1", "061");
        // Assert
        Assert.AreEqual("ConfirmOrder", ErnadPomocnici.Prikaz(rezultat).ViewName);
    }

    [Owner("Ernad"), TestMethod]
    public void ConfirmOrder_DvijeStavke_PostavljaZbirKaoUkupnuCijenu()
    {
        // Arrange - Ruza 15 x 2 + Tulipan 8 x 1
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NoviProizvod(1, "Ruza", 15), ErnadPomocnici.NoviProizvod(2, "Tulipan", 8),
            ErnadPomocnici.NovaStavka(1, 1, KorisnikId, 2), ErnadPomocnici.NovaStavka(2, 2, KorisnikId, 1));
        // Act
        var rezultat = ErnadPomocnici.Prikaz(_kontroler.ConfirmOrder("Ime", "Grad", "Adresa 1", "061"));
        // Assert
        Assert.AreEqual(38.0, (double)rezultat.ViewData["Total"]!, 0.001);
    }

    [Owner("Ernad"), TestMethod]
    public void ConfirmOrder_PodaciKupca_ProsljedjujuSePogledu()
    {
        // Act
        var rezultat = ErnadPomocnici.Prikaz(_kontroler.ConfirmOrder("Ime Prezime", "Sarajevo", "Zmaja od Bosne 1", "061111222"));
        // Assert
        Assert.AreEqual("Ime Prezime", rezultat.ViewData["Name"]);
        Assert.AreEqual("Sarajevo", rezultat.ViewData["City"]);
        Assert.AreEqual("Zmaja od Bosne 1", rezultat.ViewData["Address"]);
        Assert.AreEqual("061111222", rezultat.ViewData["Phone"]);
    }

    [Owner("Ernad"), TestMethod]
    public void ConfirmOrder_StavkeDrugogKorisnika_NeUlazeUUkupnuCijenu()
    {
        // Arrange
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NoviProizvod(), ErnadPomocnici.NovaStavka(1, 1, "k2", 5));
        // Act
        var rezultat = ErnadPomocnici.Prikaz(_kontroler.ConfirmOrder("Ime", "Grad", "Adresa 1", "061"));
        // Assert
        Assert.AreEqual(0.0, (double)rezultat.ViewData["Total"]!, 0.001);
    }

    [Owner("Ernad"), TestMethod]
    public void ConfirmOrder_PraznaKorpa_UkupnaCijenaJeNula()
    {
        // Act
        var rezultat = ErnadPomocnici.Prikaz(_kontroler.ConfirmOrder("Ime", "Grad", "Adresa 1", "061"));
        // Assert
        Assert.AreEqual(0.0, (double)rezultat.ViewData["Total"]!, 0.001);
    }

    [Owner("Ernad"), TestMethod]
    public void ConfirmOrder_AnonimniKorisnik_UkupnaCijenaJeNula()
    {
        // Arrange
        PripremiKorpu();
        ErnadPomocnici.PostaviKorisnika(_kontroler, null);
        // Act
        var rezultat = ErnadPomocnici.Prikaz(_kontroler.ConfirmOrder("Ime", "Grad", "Adresa 1", "061"));
        // Assert
        Assert.AreEqual(0.0, (double)rezultat.ViewData["Total"]!, 0.001);
    }

    [Owner("Ernad"), TestMethod]
    [DynamicData(nameof(ErnadPomocnici.StavkeKorpe), typeof(ErnadPomocnici), DynamicDataSourceType.Method)]
    public void ConfirmOrder_RazliciteCijeneIKolicine_RacunaOcekivanuUkupnuCijenu(
        string naziv, double cijena, int? kolicina, double ocekivano)
    {
        // Arrange
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NoviProizvod(1, "Proizvod", cijena),
            ErnadPomocnici.NovaStavka(1, 1, KorisnikId, kolicina));
        // Act
        var rezultat = ErnadPomocnici.Prikaz(_kontroler.ConfirmOrder("Ime", "Grad", "Adresa 1", "061"));
        // Assert
        Assert.AreEqual(ocekivano, (double)rezultat.ViewData["Total"]!, 0.001, naziv);
    }

    // ==================== SubmitOrderFinal: neuspjesni scenariji ====================

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_PraznaKorpa_PreusmjeravaNaIndex()
    {
        // Act
        var rezultat = await Potvrdi();
        // Assert
        Assert.AreEqual("Index", ErnadPomocnici.Preusmjerenje(rezultat).ActionName);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_PraznaKorpa_NeKreiraPlacanjeNiNarudzbu()
    {
        // Act
        await Potvrdi();
        // Assert
        Assert.AreEqual(0, _context.Payments.Count());
        Assert.AreEqual(0, _context.Orders.Count());
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_PraznaKorpa_NeSaljeEmail()
    {
        // Act
        await Potvrdi();
        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [Owner("Ernad"), TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow((string?)null)]
    public async Task SubmitOrderFinal_KarticaBezBrojaKartice_VracaPogledConfirmOrderSaPlacanjem(string? kartica)
    {
        // Arrange
        PripremiKorpu();
        // Act
        var rezultat = ErnadPomocnici.Prikaz(await Potvrdi("Card", kartica));
        // Assert
        Assert.AreEqual("ConfirmOrder", rezultat.ViewName);
        Assert.IsInstanceOfType<Payment>(rezultat.Model);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_KarticaBezBrojaKartice_DodajeGreskuUModelState()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi("Card", "");
        // Assert
        Assert.AreEqual("Card number is required when paying by card.",
            _kontroler.ModelState["BankAccount"]!.Errors[0].ErrorMessage);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_KarticaBezBrojaKartice_NeSnimaPlacanjeNiNarudzbu()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi("Card", "");
        // Assert
        Assert.AreEqual(0, _context.Payments.Count());
        Assert.AreEqual(0, _context.Orders.Count());
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_NeispravanModelState_VracaPodatkeKupcaPogledu()
    {
        // Arrange
        PripremiKorpu();
        _kontroler.ModelState.AddModelError("Phone", "Neispravan telefon");
        // Act
        var rezultat = ErnadPomocnici.Prikaz(await Potvrdi());
        // Assert
        Assert.AreEqual("Sarajevo", rezultat.ViewData["City"]);
        Assert.AreEqual(30.0, (double)rezultat.ViewData["Total"]!, 0.001);
        Assert.AreEqual(DatumIsporuke, rezultat.ViewData["DeliveryDate"]);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_NeispravanModelState_KorpaOstajeNepromijenjena()
    {
        // Arrange
        PripremiKorpu();
        _kontroler.ModelState.AddModelError("Phone", "Neispravan telefon");
        // Act
        await Potvrdi();
        // Assert
        Assert.AreEqual(1, _context.Cart.Count(c => c.PersonID == KorisnikId));
    }

    // ==================== SubmitOrderFinal: uspjesni scenariji ====================

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_ValidanUnos_VracaPogledOrderSuccess()
    {
        // Arrange
        PripremiKorpu();
        // Act
        var rezultat = await Potvrdi();
        // Assert
        Assert.AreEqual("OrderSuccess", ErnadPomocnici.Prikaz(rezultat).ViewName);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_ValidanUnos_PogledSadrziIdNarudzbe()
    {
        // Arrange
        PripremiKorpu();
        // Act
        var rezultat = ErnadPomocnici.Prikaz(await Potvrdi());
        // Assert
        Assert.AreEqual(_context.Orders.Single().OrderID, rezultat.ViewData["OrderId"]);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_ValidanUnos_KreiraNarudzbuSaPodacimaKupca()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi();
        // Assert
        var narudzba = _context.Orders.Single();
        Assert.AreEqual(KorisnikId, narudzba.PersonID);
        Assert.AreEqual(30.0, narudzba.TotalAmountToPay, 0.001);
        Assert.AreEqual(DatumIsporuke, narudzba.DeliveryDate);
        Assert.IsFalse(narudzba.IsOrderSent);
        Assert.IsFalse(narudzba.IsPickedUp);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_ValidanUnos_NarudzbaJePovezanaSaPlacanjem()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi();
        // Assert
        Assert.AreEqual(_context.Payments.Single().PaymentID, _context.Orders.Single().PaymentID);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_ValidanUnos_PlacanjeSadrziAdresuIIznos()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi();
        // Assert
        var placanje = _context.Payments.Single();
        Assert.AreEqual("Zmaja od Bosne 1", placanje.DeliveryAddress);
        Assert.AreEqual(30.0, placanje.PayedAmount, 0.001);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_ValidanUnos_IsprazniKorpuKorisnika()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi();
        // Assert
        Assert.AreEqual(0, _context.Cart.Count(c => c.PersonID == KorisnikId));
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_ValidanUnos_StavkeDrugogKorisnikaOstajuUKorpi()
    {
        // Arrange
        PripremiKorpu();
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NovaStavka(2, 1, "k2", 3));
        // Act
        await Potvrdi();
        // Assert
        Assert.AreEqual(1, _context.Cart.Count(c => c.PersonID == "k2"));
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_DvijeStavkeUKorpi_KreiraDvijeStavkeNarudzbe()
    {
        // Arrange
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NovaOsoba(), ErnadPomocnici.NoviProizvod(1, "Ruza", 15),
            ErnadPomocnici.NoviProizvod(2, "Tulipan", 8),
            ErnadPomocnici.NovaStavka(1, 1, KorisnikId, 2), ErnadPomocnici.NovaStavka(2, 2, KorisnikId, 1));
        // Act
        await Potvrdi();
        // Assert
        var idNarudzbe = _context.Orders.Single().OrderID;
        Assert.AreEqual(2, _context.ProductOrders.Count(po => po.OrderID == idNarudzbe));
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_StavkaBezKolicine_UpisujeKolicinuJedan()
    {
        // Arrange
        PripremiKorpu(kolicina: null);
        // Act
        await Potvrdi();
        // Assert
        Assert.AreEqual(1, _context.ProductOrders.Single().ProductQuantity);
    }

    // ---------- nacin placanja ----------

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_PlacanjePouzecem_KreiraPlacanjeGotovinom()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi("Delivery");
        // Assert
        Assert.AreEqual(PaymentType.Cash, _context.Payments.Single().PaymentType);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_PlacanjeKarticom_KreiraPlacanjeKarticomSaBrojemKartice()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi("Card", BrojKartice);
        // Assert
        var placanje = _context.Payments.Single();
        Assert.AreEqual(PaymentType.CreditCard, placanje.PaymentType);
        Assert.AreEqual(BrojKartice, placanje.BankAccount);
    }

    [Owner("Ernad"), TestMethod]
    [DynamicData(nameof(ErnadPomocnici.NaciniPlacanja), typeof(ErnadPomocnici), DynamicDataSourceType.Method)]
    public async Task SubmitOrderFinal_RazlicitiNaciniPlacanja_SnimaOcekivaniTipIKarticu(
        string naziv, string nacinPlacanja, string? kartica, PaymentType ocekivaniTip, string? ocekivanaKartica)
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi(nacinPlacanja, kartica);
        // Assert
        var placanje = _context.Payments.Single();
        Assert.AreEqual(ocekivaniTip, placanje.PaymentType, naziv);
        Assert.AreEqual(ocekivanaKartica, placanje.BankAccount, naziv);
    }

    // ---------- email (zamjenski objekat IEmailService) ----------

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_ValidanUnos_SaljeEmailPotvrdeKorisniku()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi();
        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(EmailKorisnika, "Your order was successful", It.IsAny<string>()), Times.Once());
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_ValidanUnos_EmailSadrziProizvodIUkupnuCijenu()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi();
        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.Is<string>(tijelo => tijelo.Contains("- Ruza x 2 = 30 USD") && tijelo.Contains("Sarajevo"))), Times.Once());
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_PlacanjePouzecem_EmailNavodiPlacanjeGotovinom()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi("Delivery");
        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.Is<string>(tijelo => tijelo.Contains("Cash on Delivery"))), Times.Once());
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_PlacanjeKarticom_EmailNavodiKreditnuKarticu()
    {
        // Arrange
        PripremiKorpu();
        // Act
        await Potvrdi("Card", BrojKartice);
        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.Is<string>(tijelo => tijelo.Contains("Credit Card"))), Times.Once());
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_KorisnikNePostojiUBazi_NeSaljeEmail()
    {
        // Arrange - korpa postoji, ali korisnik k1 nije u bazi
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NoviProizvod(), ErnadPomocnici.NovaStavka(1, 1, KorisnikId, 2));
        // Act
        await Potvrdi();
        // Assert
        _emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_KorisnikNePostojiUBazi_IpakKreiraNarudzbu()
    {
        // Arrange
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NoviProizvod(), ErnadPomocnici.NovaStavka(1, 1, KorisnikId, 2));
        // Act
        await Potvrdi();
        // Assert
        Assert.AreEqual(1, _context.Orders.Count());
    }

    // ---------- izuzeci ----------

    [Owner("Ernad"), TestMethod]
    public async Task SubmitOrderFinal_EmailServisBacaIzuzetak_IzuzetakSePropagira()
    {
        // Arrange
        PripremiKorpu();
        PostaviEmailKojiBacaIzuzetak();
        // Act + Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Potvrdi());
    }

    [Owner("Ernad"), TestMethod]
    [TestCategory("PostojecePonasanje")]
    [Description("Kad slanje maila pukne, placanje je vec snimljeno, a narudzba nije (nema transakcije).")]
    public async Task SubmitOrderFinal_EmailServisBacaIzuzetak_PlacanjeOstajeSnimljenoBezNarudzbe()
    {
        // Arrange
        PripremiKorpu();
        PostaviEmailKojiBacaIzuzetak();
        // Act
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Potvrdi());
        // Assert
        Assert.AreEqual(1, _context.Payments.Count());
        Assert.AreEqual(0, _context.Orders.Count());
    }

    // ==================== SubmitRating ====================

    [Owner("Ernad"), TestMethod]
    [DynamicData(nameof(ErnadPomocnici.ValidneOcjene), typeof(ErnadPomocnici), DynamicDataSourceType.Method)]
    public async Task SubmitRating_ValidnaOcjena_SnimaOcjenuUNarudzbu(double ocjena)
    {
        // Arrange
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NovaNarudzba(7));
        // Act
        await _kontroler.SubmitRating(7, ocjena);
        // Assert
        Assert.AreEqual(ocjena, _context.Orders.Single().Rating);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitRating_ValidnaOcjena_VracaPogledFinishRating()
    {
        // Arrange
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NovaNarudzba(7));
        // Act
        var rezultat = await _kontroler.SubmitRating(7, 4);
        // Assert
        Assert.AreEqual("~/Views/Home/FinishRating.cshtml", ErnadPomocnici.Prikaz(rezultat).ViewName);
    }

    [Owner("Ernad"), TestMethod]
    [DynamicData(nameof(ErnadPomocnici.NevalidneOcjene), typeof(ErnadPomocnici), DynamicDataSourceType.Method)]
    public async Task SubmitRating_NevalidnaOcjena_PreusmjeravaNaOrderSuccess(double ocjena)
    {
        // Act
        var rezultat = await _kontroler.SubmitRating(7, ocjena);
        // Assert
        Assert.AreEqual("OrderSuccess", ErnadPomocnici.Preusmjerenje(rezultat).ActionName);
    }

    [Owner("Ernad"), TestMethod]
    [DynamicData(nameof(ErnadPomocnici.NevalidneOcjene), typeof(ErnadPomocnici), DynamicDataSourceType.Method)]
    public async Task SubmitRating_NevalidnaOcjena_PostavljaPorukuGreskeUTempData(double ocjena)
    {
        // Act
        await _kontroler.SubmitRating(7, ocjena);
        // Assert
        Assert.AreEqual("Invalid rating value.", _kontroler.TempData["RatingError"]);
    }

    [Owner("Ernad"), TestMethod]
    [DynamicData(nameof(ErnadPomocnici.NevalidneOcjene), typeof(ErnadPomocnici), DynamicDataSourceType.Method)]
    public async Task SubmitRating_NevalidnaOcjena_NarudzbaOstajeBezOcjene(double ocjena)
    {
        // Arrange
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NovaNarudzba(7));
        // Act
        await _kontroler.SubmitRating(7, ocjena);
        // Assert
        Assert.IsNull(_context.Orders.Single().Rating);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitRating_NarudzbaNePostoji_VracaNotFound()
    {
        // Act
        var rezultat = await _kontroler.SubmitRating(999, 4);
        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [Owner("Ernad"), TestMethod]
    public async Task SubmitRating_PonovnaOcjena_PrepisujePrethodnuOcjenu()
    {
        // Arrange
        var narudzba = ErnadPomocnici.NovaNarudzba(7);
        narudzba.Rating = 2;
        ErnadPomocnici.Snimi(_context, narudzba);
        // Act
        await _kontroler.SubmitRating(7, 5);
        // Assert
        Assert.AreEqual(5.0, _context.Orders.Single().Rating);
    }

    [Owner("Ernad"), TestMethod]
    [TestCategory("PostojecePonasanje")]
    [Description("SubmitRating ne provjerava vlasnika narudzbe: bilo ko moze ocijeniti tudju narudzbu.")]
    public async Task SubmitRating_NarudzbaDrugogKorisnika_IpakSnimaOcjenu()
    {
        // Arrange
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NovaNarudzba(7, "k2"));
        // Act
        await _kontroler.SubmitRating(7, 1);
        // Assert
        Assert.AreEqual(1.0, _context.Orders.Single().Rating);
    }

    [Owner("Ernad"), TestMethod]
    [TestCategory("PostojecePonasanje")]
    [Description("Vrijednost NaN prolazi provjeru (NaN < 1 i NaN > 5 su false) i upisuje se kao ocjena.")]
    public async Task SubmitRating_OcjenaNaN_PrihvataSeKaoValidna()
    {
        // Arrange
        ErnadPomocnici.Snimi(_context, ErnadPomocnici.NovaNarudzba(7));
        // Act
        await _kontroler.SubmitRating(7, double.NaN);
        // Assert
        Assert.IsTrue(double.IsNaN(_context.Orders.Single().Rating!.Value));
    }

    // ==================== FinishRating ====================

    [Owner("Ernad"), TestMethod]
    public void FinishRating_Pozvan_VracaPrikazBezModela()
    {
        // Act
        var rezultat = ErnadPomocnici.Prikaz(_kontroler.FinishRating());
        // Assert
        Assert.IsNull(rezultat.Model);
    }

    [Owner("Ernad"), TestMethod]
    public void FinishRating_Pozvan_KoristiZadaniPogled()
    {
        // Act
        var rezultat = ErnadPomocnici.Prikaz(_kontroler.FinishRating());
        // Assert
        Assert.IsNull(rezultat.ViewName);
    }
}
