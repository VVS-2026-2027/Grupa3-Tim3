using System.Globalization;
using System.Security.Claims;
using System.Xml.Linq;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Cvjecara_Latica.Tests.ErnadTest;

/// <summary>
/// Pomocne metode za testove Clana 3 (IEmailService, EmailService, UnclaimedOrderChecker,
/// CartsController II dio). Sva infrastruktura (petlje, parsiranje XML-a) je ovdje,
/// tako da testne metode ostaju bez programske logike.
/// </summary>
public static class ErnadPomocnici
{
    public const string KorisnikId = "k1";

    // ---------------------------------------------------------------- entiteti

    public static Person NovaOsoba(string id = KorisnikId) => new()
    {
        Id = id,
        UserName = id,
        NormalizedUserName = id.ToUpperInvariant(),
        Email = id + "@test.com",
        NormalizedEmail = (id + "@test.com").ToUpperInvariant(),
        FirstName = "Test",
        LastName = "Korisnik",
        HomeAdress = "Test Ulica 10"
    };

    public static Product NoviProizvod(int id = 1, string naziv = "Ruza", double cijena = 15.0) => new()
    {
        ProductID = id,
        Name = naziv,
        ImageUrl = "/img/proizvod.jpg",
        Price = cijena,
        Stock = 20,
        Category = "Cvijece",
        Description = "Opis proizvoda za testiranje",
        productType = "Flower",
        Color = "Red"
    };

    public static Cart NovaStavka(int id, int proizvodId, string osobaId = KorisnikId, int? kolicina = 2) => new()
    {
        CartID = id,
        ProductID = proizvodId,
        PersonID = osobaId,
        ProductQuantity = kolicina
    };

    public static Order NovaNarudzba(int id, string osobaId = KorisnikId) => new()
    {
        OrderID = id,
        PersonID = osobaId,
        PaymentID = 1,
        purchaseDate = new DateTime(2026, 11, 15, 12, 0, 0),
        TotalAmountToPay = 30.0,
        DeliveryDate = new DateTime(2026, 11, 20, 10, 0, 0)
    };

    /// <summary>Upisuje entitete u zamjensku (InMemory) bazu i cisti tracker.</summary>
    public static void Snimi(ApplicationDbContext baza, params object[] entiteti)
    {
        baza.AddRange(entiteti);
        baza.SaveChanges();
        baza.ChangeTracker.Clear();
    }

    // ---------------------------------------------------------------- HTTP kontekst i TempData

    /// <summary>Postavlja prijavljenog korisnika; id = null daje anonimnog korisnika.</summary>
    public static void PostaviKorisnika(Controller kontroler, string? id)
    {
        var claimovi = new List<Claim>();
        if (id is not null)
            claimovi.Add(new Claim(ClaimTypes.NameIdentifier, id));

        kontroler.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claimovi, id is null ? null : "Test"))
            }
        };
    }

    /// <summary>TempData sa zamjenskim ITempDataProvider-om (bez stvarnih sesija i cookieja).</summary>
    public static TempDataDictionary NoviTempData()
    {
        var provider = new Mock<ITempDataProvider>();
        provider.Setup(p => p.LoadTempData(It.IsAny<HttpContext>()))
            .Returns(new Dictionary<string, object>());
        return new TempDataDictionary(new DefaultHttpContext(), provider.Object);
    }

    // ---------------------------------------------------------------- provjere rezultata

    public static ViewResult Prikaz(IActionResult rezultat)
    {
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        return (ViewResult)rezultat;
    }

    public static RedirectToActionResult Preusmjerenje(IActionResult rezultat)
    {
        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        return (RedirectToActionResult)rezultat;
    }

    // ---------------------------------------------------------------- zamjenski IConfiguration

    /// <summary>
    /// Zamjenski objekat Mock&lt;IConfiguration&gt; sa postavkama EmailSettings.
    /// Vrijednost null znaci da postavka nije definisana (indekser vraca null).
    /// </summary>
    public static Mock<IConfiguration> NapraviKonfiguraciju(string? smtpServer, string? smtpPort, string? senderEmail)
    {
        var konfiguracija = new Mock<IConfiguration>();
        PostaviVrijednost(konfiguracija, "EmailSettings:SmtpServer", smtpServer);
        PostaviVrijednost(konfiguracija, "EmailSettings:SmtpPort", smtpPort);
        PostaviVrijednost(konfiguracija, "EmailSettings:SenderEmail", senderEmail);
        PostaviVrijednost(konfiguracija, "EmailSettings:SenderName", "Latica");
        PostaviVrijednost(konfiguracija, "EmailSettings:Username", "korisnik");
        PostaviVrijednost(konfiguracija, "EmailSettings:Password", "lozinka");
        return konfiguracija;
    }

    private static void PostaviVrijednost(Mock<IConfiguration> konfiguracija, string kljuc, string? vrijednost)
        => konfiguracija.Setup(c => c[kljuc]).Returns(vrijednost);

    // ---------------------------------------------------------------- XML izvori podataka

    private static IEnumerable<XElement> Slucajevi(string datoteka, string element)
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestData", "Ernad", datoteka))
            .Root!.Elements(element).ToList();

    private static string? Tekst(XElement e, string naziv) => (string?)e.Attribute(naziv);

    private static double Broj(XElement e, string naziv)
        => double.Parse(Tekst(e, naziv)!, CultureInfo.InvariantCulture);

    private static int Cijeli(XElement e, string naziv)
        => int.Parse(Tekst(e, naziv)!, CultureInfo.InvariantCulture);

    private static bool Logicka(XElement e, string naziv) => bool.Parse(Tekst(e, naziv)!);

    // email_konfiguracije.xml: naziv, smtpServer, port, senderEmail, primalac, izuzetak
    public static IEnumerable<object?[]> EmailKonfiguracije()
        => Slucajevi("email_konfiguracije.xml", "slucaj")
            .Select(e => new object?[]
            {
                Tekst(e, "naziv"),
                Tekst(e, "smtpServer"),
                Tekst(e, "port"),
                Tekst(e, "senderEmail"),
                Tekst(e, "primalac"),
                Type.GetType(Tekst(e, "izuzetak")!, true)
            }).ToList();

    // podsjetnici.xml: naziv, satiKasnjenja, preuzeta, lockoutOmogucen, ocekivaniPodsjetnici, ocekivaneDeaktivacije
    public static IEnumerable<object?[]> PodsjetniciSlucajevi()
        => Slucajevi("podsjetnici.xml", "slucaj")
            .Select(e => new object?[]
            {
                Tekst(e, "naziv"),
                Broj(e, "satiKasnjenja"),
                Logicka(e, "preuzeta"),
                Logicka(e, "lockoutOmogucen"),
                Cijeli(e, "ocekivaniPodsjetnici"),
                Cijeli(e, "ocekivaneDeaktivacije")
            }).ToList();

    // nacini_placanja.xml: naziv, paymentTypeString, bankAccount, ocekivaniTip, ocekivanaKartica
    public static IEnumerable<object?[]> NaciniPlacanja()
        => Slucajevi("nacini_placanja.xml", "slucaj")
            .Select(e => new object?[]
            {
                Tekst(e, "naziv"),
                Tekst(e, "paymentTypeString"),
                Tekst(e, "bankAccount"),
                Enum.Parse<PaymentType>(Tekst(e, "ocekivaniTip")!),
                Tekst(e, "ocekivanaKartica")
            }).ToList();

    // korpa_ukupno.xml: naziv, cijena, kolicina (izostavljena = null), ocekivano
    public static IEnumerable<object?[]> StavkeKorpe()
        => Slucajevi("korpa_ukupno.xml", "stavka")
            .Select(e => new object?[]
            {
                Tekst(e, "naziv"),
                Broj(e, "cijena"),
                Tekst(e, "kolicina") is null ? null : Cijeli(e, "kolicina"),
                Broj(e, "ocekivano")
            }).ToList();

    // ocjene.xml: vrijednost, validna
    public static IEnumerable<object?[]> ValidneOcjene() => Ocjene(true);

    public static IEnumerable<object?[]> NevalidneOcjene() => Ocjene(false);

    private static IEnumerable<object?[]> Ocjene(bool validna)
        => Slucajevi("ocjene.xml", "ocjena")
            .Where(e => Logicka(e, "validna") == validna)
            .Select(e => new object?[] { Broj(e, "vrijednost") })
            .ToList();
}
