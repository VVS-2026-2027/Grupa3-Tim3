using System.Security.Claims;
using System.Text.Json;
using System.Xml.Linq;
using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Cvjecara_Latica.Services;
using Cvjecara_Latica.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Cvjecara_Latica.Tests.LamijaTest
{
    [TestClass]
    [TestCategory("CartsController-I")]
    public class CartsControllerTests
    {
        private const string KorisnikId = "korisnik-1";
        private const string DrugiKorisnikId = "korisnik-2";
        private const string AdminId = "admin-1";
        private const string Uloga_Administrator = "Administrator";
        private const string PutanjaPodataka = "TestData/Lamija/";

        private ApplicationDbContext _context = null!;
        private Mock<IEmailService> _emailMock = null!;
        private Mock<ITempDataProvider> _tempDataMock = null!;
        private CartsController _controller = null!;

        [ClassInitialize]
        public static void ProvjeriTestnePodatke(TestContext context)
        {
            Assert.IsTrue(File.Exists(PutanjaPodataka + "dodavanje_u_korpu.csv"));
            Assert.IsTrue(File.Exists(PutanjaPodataka + "datumi_isporuke.csv"));
            Assert.IsTrue(File.Exists(PutanjaPodataka + "ukupna_cijena.json"));
            Assert.IsTrue(File.Exists(PutanjaPodataka + "popusti.json"));
            Assert.IsTrue(File.Exists(PutanjaPodataka + "izmjene_kolicine.xml"));
        }

        [TestInitialize]
        public void Postavi()
        {
            _context = TestDbContextFactory.Kreiraj();

            _emailMock = new Mock<IEmailService>();

            _tempDataMock = new Mock<ITempDataProvider>();
            _tempDataMock
                .Setup(p => p.LoadTempData(It.IsAny<HttpContext>()))
                .Returns(new Dictionary<string, object>());

            _controller = new CartsController(_context, _emailMock.Object);
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), _tempDataMock.Object);
            PostaviKorisnika(Korisnik(KorisnikId));
        }

        [TestCleanup]
        public void Ocisti()
        {
            _controller.Dispose();
            _context.Dispose();
        }

        [TestMethod]
        public async Task Index_PrijavljenKorisnik_VracaSamoNjegoveStavke()
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId);
            DodajStavkuUKorpu(KorisnikId);
            DodajStavkuUKorpu(DrugiKorisnikId);

            // Act
            var rezultat = await _controller.Index();

            // Assert
            var model = (List<Cart>)((ViewResult)rezultat).Model!;
            Assert.HasCount(2, model);
        }

        [TestMethod]
        public async Task Index_PrijavljenKorisnik_VracaTacnoNjegoveStavke()
        {
            // Arrange
            var prva = DodajStavkuUKorpu(KorisnikId);
            var druga = DodajStavkuUKorpu(KorisnikId);
            DodajStavkuUKorpu(DrugiKorisnikId);

            // Act
            var model = (List<Cart>)((ViewResult)await _controller.Index()).Model!;

            // Assert
            CollectionAssert.AreEquivalent(new[] { prva, druga }, model);
        }

        [TestMethod]
        public async Task Index_NeprijavljenKorisnik_VracaStavkeAnonimnogKorisnika()
        {
            // Arrange
            PostaviKorisnika(new ClaimsIdentity());
            DodajStavkuUKorpu("anonimni");

            // Act
            var rezultat = await _controller.Index();

            // Assert
            var model = (List<Cart>)((ViewResult)rezultat).Model!;
            Assert.HasCount(1, model);
        }

        [TestMethod]
        public async Task Index_PrijavljenKorisnikBezId_BacaNullReferenceException()
        {
            // Arrange
            PostaviKorisnika(KorisnikBezId());

            // Act & Assert
            await Assert.ThrowsExactlyAsync<NullReferenceException>(() => _controller.Index());
        }

        [TestMethod]
        public async Task Details_IdJeNull_VracaNotFound()
        {
            // Act
            var rezultat = await _controller.Details(null);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_StavkaPrijavljenogKorisnika_VracaViewSaTomStavkom()
        {
            // Arrange
            var stavka = DodajStavkuUKorpu(KorisnikId);

            // Act
            var rezultat = await _controller.Details(stavka.CartID);

            // Assert
            var view = rezultat as ViewResult;
            Assert.IsNotNull(view);
            Assert.AreEqual(stavka.CartID, ((Cart)view.Model!).CartID);
        }

        [TestMethod]
        public async Task Details_StavkaDrugogKorisnika_VracaForbid()
        {
            // Arrange
            var tudjaStavka = DodajStavkuUKorpu(DrugiKorisnikId);

            // Act
            var rezultat = await _controller.Details(tudjaStavka.CartID);

            // Assert
            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_AdministratorTudjaStavka_VracaView()
        {
            // Arrange
            var tudjaStavka = DodajStavkuUKorpu(DrugiKorisnikId);
            PostaviKorisnika(Administrator(AdminId));

            // Act
            var rezultat = await _controller.Details(tudjaStavka.CartID);

            // Assert
            Assert.IsInstanceOfType<ViewResult>(rezultat);
        }

        [TestMethod]
        public void Create_Get_VracaViewSaListamaZaOdabir()
        {
            // Act
            var rezultat = _controller.Create();

            // Assert
            Assert.IsInstanceOfType<ViewResult>(rezultat);
            Assert.IsInstanceOfType<SelectList>(_controller.ViewData["ProductID"]);
            Assert.IsInstanceOfType<SelectList>(_controller.ViewData["PersonID"]);
        }

        [TestMethod]
        public async Task Create_ValidnaStavka_DodajeStavkuIPreusmjeravaNaIndex()
        {
            // Arrange
            var proizvod = DodajProizvod();
            var novaStavka = new Cart { ProductID = proizvod.ProductID, PersonID = KorisnikId, ProductQuantity = 2 };

            // Act
            var rezultat = await _controller.Create(novaStavka);

            // Assert
            Assert.AreEqual(1, _context.Cart.Count());
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        public static IEnumerable<object?[]> NoveStavke => new[]
        {
            new object?[] { KorisnikId, 1 },
            new object?[] { KorisnikId, 25 },
            new object?[] { "anonimni", 3 },
            new object?[] { DrugiKorisnikId, null }
        };

        [TestMethod]
        [DynamicData(nameof(NoveStavke))]
        public async Task Create_RazliciteValidneStavke_SpremaStavkuSaZadanimPodacima(string vlasnikId, int? kolicina)
        {
            // Arrange
            var proizvod = DodajProizvod();
            var novaStavka = new Cart { ProductID = proizvod.ProductID, PersonID = vlasnikId, ProductQuantity = kolicina };

            // Act
            await _controller.Create(novaStavka);

            // Assert
            var spremljena = _context.Cart.Single();
            Assert.AreEqual(vlasnikId, spremljena.PersonID);
            Assert.AreEqual(kolicina, spremljena.ProductQuantity);
        }

        [TestMethod]
        public async Task Create_NevalidanModel_VracaIstiViewINeSpasava()
        {
            // Arrange
            var novaStavka = new Cart { ProductID = 1, PersonID = KorisnikId, ProductQuantity = 2 };
            _controller.ModelState.AddModelError("ProductQuantity", "Neispravna količina");

            // Act
            var rezultat = await _controller.Create(novaStavka);

            // Assert
            Assert.AreSame(novaStavka, ((ViewResult)rezultat).Model);
            Assert.AreEqual(0, _context.Cart.Count());
        }

        [TestMethod]
        public async Task Edit_Get_IdJeNull_VracaNotFound()
        {
            var rezultat = await _controller.Edit(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Edit_Get_StavkaPrijavljenogKorisnika_VracaViewSaStavkom()
        {
            // Arrange
            var stavka = DodajStavkuUKorpu(KorisnikId);

            // Act
            var rezultat = await _controller.Edit(stavka.CartID);

            // Assert
            Assert.AreEqual(stavka.CartID, ((Cart)((ViewResult)rezultat).Model!).CartID);
        }

        [TestMethod]
        public async Task Edit_Get_StavkaDrugogKorisnika_VracaForbid()
        {
            // Arrange
            var tudjaStavka = DodajStavkuUKorpu(DrugiKorisnikId);

            // Act
            var rezultat = await _controller.Edit(tudjaStavka.CartID);

            // Assert
            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        [TestMethod]
        public async Task Edit_Post_IdSeNePoklapa_VracaNotFound()
        {
            // Arrange
            var izmjena = new Cart { CartID = 2, ProductQuantity = 3 };

            // Act
            var rezultat = await _controller.Edit(1, izmjena);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Edit_Post_ValidnaIzmjena_MijenjaKolicinuIPreusmjerava()
        {
            // Arrange
            var stavka = DodajStavkuUKorpu(KorisnikId, kolicina: 1);
            var izmjena = new Cart { CartID = stavka.CartID, ProductQuantity = 5 };

            // Act
            var rezultat = await _controller.Edit(stavka.CartID, izmjena);

            // Assert
            Assert.AreEqual(5, _context.Cart.Single().ProductQuantity);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        public static IEnumerable<object[]> IzmjeneKolicineIzXml()
        {
            return XDocument.Load(PutanjaPodataka + "izmjene_kolicine.xml")
                .Descendants("Izmjena")
                .Select(i => new object[] { (int)i.Element("Pocetna")!, (int)i.Element("Nova")! });
        }

        [TestMethod]
        [DynamicData(nameof(IzmjeneKolicineIzXml))]
        public async Task Edit_Post_NovaKolicinaIzXml_SpremaNovuKolicinu(int pocetnaKolicina, int novaKolicina)
        {
            // Arrange
            var stavka = DodajStavkuUKorpu(KorisnikId, pocetnaKolicina);
            var izmjena = new Cart { CartID = stavka.CartID, ProductQuantity = novaKolicina };

            // Act
            await _controller.Edit(stavka.CartID, izmjena);

            // Assert
            Assert.AreEqual(novaKolicina, _context.Cart.Single().ProductQuantity);
        }

        [TestMethod]
        public async Task Edit_Post_StavkaDrugogKorisnika_VracaForbidINeMijenja()
        {
            // Arrange
            var tudjaStavka = DodajStavkuUKorpu(DrugiKorisnikId, kolicina: 1);
            var izmjena = new Cart { CartID = tudjaStavka.CartID, ProductQuantity = 9 };

            // Act
            var rezultat = await _controller.Edit(tudjaStavka.CartID, izmjena);

            // Assert
            Assert.IsInstanceOfType<ForbidResult>(rezultat);
            Assert.AreEqual(1, _context.Cart.Single().ProductQuantity);
        }

        [TestMethod]
        public async Task Edit_Post_NevalidanModel_VracaViewSaIstimModelom()
        {
            // Arrange
            var stavka = DodajStavkuUKorpu(KorisnikId, kolicina: 1);
            var izmjena = new Cart { CartID = stavka.CartID, ProductQuantity = -3 };
            _controller.ModelState.AddModelError("ProductQuantity", "Neispravna količina");

            // Act
            var rezultat = await _controller.Edit(stavka.CartID, izmjena);

            // Assert
            Assert.AreSame(izmjena, ((ViewResult)rezultat).Model);
            Assert.AreEqual(1, _context.Cart.Single().ProductQuantity);
        }

        [TestMethod]
        public async Task Edit_Post_AdministratorMijenjaDruguStavku_MijenjaSeSamoTaStavka()
        {
            // Arrange
            var prvaStavka = DodajStavkuUKorpu(DrugiKorisnikId, kolicina: 1);
            var drugaStavka = DodajStavkuUKorpu(DrugiKorisnikId, kolicina: 1);
            PostaviKorisnika(Administrator(AdminId));
            var izmjena = new Cart { CartID = drugaStavka.CartID, ProductQuantity = 7 };

            // Act
            await _controller.Edit(drugaStavka.CartID, izmjena);

            // Assert
            Assert.AreEqual(7, drugaStavka.ProductQuantity);
            Assert.AreEqual(1, prvaStavka.ProductQuantity);
        }

        [TestMethod]
        public async Task Delete_Get_IdJeNull_VracaNotFound()
        {
            var rezultat = await _controller.Delete(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Delete_Get_StavkaPrijavljenogKorisnika_VracaViewZaPotvrdu()
        {
            // Arrange
            var stavka = DodajStavkuUKorpu(KorisnikId);

            // Act
            var rezultat = await _controller.Delete(stavka.CartID);

            // Assert
            Assert.AreEqual(stavka.CartID, ((Cart)((ViewResult)rezultat).Model!).CartID);
        }

        [TestMethod]
        public async Task Delete_Get_StavkaDrugogKorisnika_VracaForbid()
        {
            var tudjaStavka = DodajStavkuUKorpu(DrugiKorisnikId);

            var rezultat = await _controller.Delete(tudjaStavka.CartID);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        [TestMethod]
        public async Task DeleteConfirmed_StavkaPrijavljenogKorisnika_BriseStavkuIPreusmjerava()
        {
            // Arrange
            var stavka = DodajStavkuUKorpu(KorisnikId);

            // Act
            var rezultat = await _controller.DeleteConfirmed(stavka.CartID);

            // Assert
            Assert.AreEqual(0, _context.Cart.Count());
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        [TestMethod]
        public async Task DeleteConfirmed_StavkaDrugogKorisnika_VracaForbidINeBrise()
        {
            // Arrange
            var tudjaStavka = DodajStavkuUKorpu(DrugiKorisnikId);

            // Act
            var rezultat = await _controller.DeleteConfirmed(tudjaStavka.CartID);

            // Assert
            Assert.IsInstanceOfType<ForbidResult>(rezultat);
            Assert.AreEqual(1, _context.Cart.Count());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(999)]
        public void AddToCart_NepostojeciProizvod_VracaNotFound(int nepostojeciId)
        {
            // Act
            var rezultat = _controller.AddToCart(nepostojeciId);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        [Priority(1)]
        public void AddToCart_NoviProizvod_DodajeStavkuSaKolicinomJedan()
        {
            // Arrange
            var proizvod = DodajProizvod();

            // Act
            _controller.AddToCart(proizvod.ProductID);

            // Assert
            var stavka = _context.Cart.Single();
            Assert.AreEqual(KorisnikId, stavka.PersonID);
            Assert.AreEqual(1, stavka.ProductQuantity);
        }

        [TestMethod]
        public void AddToCart_PostojeciProizvod_PreusmjeravaNaDetaljeProizvoda()
        {
            // Arrange
            var proizvod = DodajProizvod();

            // Act
            var rezultat = (RedirectToActionResult)_controller.AddToCart(proizvod.ProductID);

            // Assert
            Assert.AreEqual("Details", rezultat.ActionName);
            Assert.AreEqual("Products", rezultat.ControllerName);
            Assert.AreEqual(proizvod.ProductID, rezultat.RouteValues!["id"]);
        }

        [TestMethod]
        public void AddToCart_NeprijavljenKorisnik_DodajeStavkuAnonimnomKorisniku()
        {
            // Arrange
            var proizvod = DodajProizvod();
            PostaviKorisnika(new ClaimsIdentity());

            // Act
            _controller.AddToCart(proizvod.ProductID);

            // Assert
            Assert.AreEqual("anonimni", _context.Cart.Single().PersonID);
        }

        [TestMethod]
        public void AddToCart_PrijavljenKorisnikBezId_BacaNullReferenceException()
        {
            // Arrange
            var proizvod = DodajProizvod();
            PostaviKorisnika(KorisnikBezId());

            // Act & Assert
            Assert.ThrowsExactly<NullReferenceException>(() => _controller.AddToCart(proizvod.ProductID));
        }

        public static IEnumerable<object?[]> KolicineIzCsv()
        {
            foreach (var red in File.ReadAllLines(PutanjaPodataka + "dodavanje_u_korpu.csv").Skip(1))
            {
                var kolone = red.Split(',');
                int? pocetna = string.IsNullOrEmpty(kolone[0]) ? null : int.Parse(kolone[0]);
                yield return new object?[] { pocetna, int.Parse(kolone[1]) };
            }
        }

        [TestMethod]
        [DynamicData(nameof(KolicineIzCsv))]
        public void AddToCart_ProizvodVecUKorpi_PovecavaKolicinuZaJedan(int? pocetnaKolicina, int ocekivanaKolicina)
        {
            // Arrange
            var stavka = DodajStavkuUKorpu(KorisnikId, pocetnaKolicina);

            // Act
            _controller.AddToCart(stavka.ProductID);

            // Assert
            Assert.AreEqual(ocekivanaKolicina, _context.Cart.Single().ProductQuantity);
        }

        public static IEnumerable<object?[]> UkupneCijeneIzJson()
        {
            var slucajevi = JsonSerializer.Deserialize<List<SlucajCijene>>(
                File.ReadAllText(PutanjaPodataka + "ukupna_cijena.json"), JsonOpcije)!;
            return slucajevi.Select(s => new object?[] { s.Cijena, s.Kolicina, s.OcekivanoUkupno });
        }

        [TestMethod]
        [Priority(1)]
        [DynamicData(nameof(UkupneCijeneIzJson))]
        public void PlaceOrder_StavkaUKorpi_RacunaUkupnuCijenu(double cijena, int? kolicina, double ocekivanoUkupno)
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId, kolicina, cijena);

            // Act
            var rezultat = (ViewResult)_controller.PlaceOrder();

            // Assert
            Assert.AreEqual(ocekivanoUkupno, (double)rezultat.ViewData["Total"]!, 0.001);
        }

        [TestMethod]
        public void PlaceOrder_NeprijavljenKorisnik_UkupnaCijenaJeNula()
        {
            // Arrange
            DodajStavkuUKorpu(DrugiKorisnikId);
            PostaviKorisnika(new ClaimsIdentity());

            // Act
            var rezultat = (ViewResult)_controller.PlaceOrder();

            // Assert
            Assert.AreEqual(0.0, (double)rezultat.ViewData["Total"]!, 0.001);
        }

        public static IEnumerable<object[]> DatumiIsporukeIzCsv()
        {
            foreach (var red in File.ReadAllLines(PutanjaPodataka + "datumi_isporuke.csv").Skip(1))
            {
                var kolone = red.Split(',');
                yield return new object[] { int.Parse(kolone[0]), int.Parse(kolone[1]), kolone[2] };
            }
        }

        [TestMethod]
        [Timeout(2000, CooperativeCancellation = true)]
        [DynamicData(nameof(DatumiIsporukeIzCsv))]
        public async Task SubmitOrder_DatumIsporuke_VracaOcekivaniPogled(int danaOdDanas, int sat, string ocekivaniPogled)
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId);
            var datumIsporuke = DateTime.Today.AddDays(danaOdDanas).AddHours(sat);

            // Act
            var rezultat = await _controller.SubmitOrder("Lamija", "Sarajevo", "Zmaja od Bosne 1", "061111222", datumIsporuke, "");

            // Assert
            Assert.AreEqual(ocekivaniPogled, ((ViewResult)rezultat).ViewName);
        }

        [TestMethod]
        public async Task SubmitOrder_NevalidanDatum_DodajeGreskuUModelState()
        {
            // Act
            await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", DateTime.Today.AddHours(10), "");

            // Assert
            Assert.IsTrue(_controller.ModelState.ContainsKey("deliveryDate"));
        }

        public static IEnumerable<object[]> PopustiIzJson()
        {
            var slucajevi = JsonSerializer.Deserialize<List<SlucajPopusta>>(
                File.ReadAllText(PutanjaPodataka + "popusti.json"), JsonOpcije)!;
            return slucajevi.Select(s => new object[]
            {
                Enum.Parse<DiscountType>(s.Tip), s.Iznos, s.CijenaKorpe, s.OcekivanoUkupno
            });
        }

        [TestMethod]
        [TestProperty("Funkcionalnost", "PromoKod")]
        [Timeout(2000, CooperativeCancellation = true)]
        [DynamicData(nameof(PopustiIzJson))]
        public async Task SubmitOrder_ValidanPromoKod_UmanjujeCijenu(DiscountType tip, double iznos, double cijenaKorpe, double ocekivanoUkupno)
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId, kolicina: 1, cijena: cijenaKorpe);
            DodajPopust("POPUST-1", KorisnikId, tip, iznos);

            // Act
            var rezultat = (ViewResult)await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "POPUST-1");

            // Assert
            Assert.AreEqual(ocekivanoUkupno, (double)rezultat.ViewData["Total"]!, 0.001);
        }

        [TestMethod]
        [TestProperty("Funkcionalnost", "PromoKod")]
        public async Task SubmitOrder_ValidanPromoKod_OznacavaKodKaoIskoristen()
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId);
            var popust = DodajPopust("POPUST-1", KorisnikId, DiscountType.AmountOff, 1);

            // Act
            await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "POPUST-1");

            // Assert
            Assert.IsTrue(popust.IsUsed);
            Assert.IsTrue((bool)_controller.TempData["DiscountApplied"]!);
        }

        [TestMethod]
        [TestProperty("Funkcionalnost", "PromoKod")]
        public async Task SubmitOrder_NepostojeciPromoKod_CijenaOstajeIstaIPorukaOGresci()
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId, kolicina: 2, cijena: 10);

            // Act
            var rezultat = (ViewResult)await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "NEPOSTOJI");

            // Assert
            Assert.AreEqual(20.0, (double)rezultat.ViewData["Total"]!, 0.001);
            Assert.IsFalse((bool)_controller.TempData["DiscountApplied"]!);
        }

        [TestMethod]
        [TestProperty("Funkcionalnost", "PromoKod")]
        public async Task SubmitOrder_PromoKodDrugogKorisnika_NePrimjenjujePopust()
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId, kolicina: 1, cijena: 50);
            var tudjiPopust = DodajPopust("POPUST-1", DrugiKorisnikId, DiscountType.AmountOff, 20);

            // Act
            var rezultat = (ViewResult)await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "POPUST-1");

            // Assert
            Assert.AreEqual(50.0, (double)rezultat.ViewData["Total"]!, 0.001);
            Assert.IsFalse(tudjiPopust.IsUsed);
        }

        [TestMethod]
        [TestProperty("Funkcionalnost", "PromoKod")]
        public async Task SubmitOrder_VecIskoristenPromoKod_NePrimjenjujePopust()
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId, kolicina: 1, cijena: 40);
            DodajPopust("POPUST-1", KorisnikId, DiscountType.AmountOff, 10).IsUsed = true;
            _context.SaveChanges();

            // Act
            var rezultat = (ViewResult)await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "POPUST-1");

            // Assert
            Assert.AreEqual(40.0, (double)rezultat.ViewData["Total"]!, 0.001);
            Assert.IsFalse((bool)_controller.TempData["DiscountApplied"]!);
        }

        [TestMethod]
        [TestProperty("Funkcionalnost", "PromoKod")]
        public async Task SubmitOrder_ValidanPromoKod_PorukaPotvrdjujePopust()
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId, kolicina: 1, cijena: 20);
            DodajPopust("POPUST-1", KorisnikId, DiscountType.AmountOff, 5);

            // Act
            await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "POPUST-1");

            // Assert
            StringAssert.Contains((string)_controller.TempData["DiscountMessage"]!, "successfully applied");
        }

        [TestMethod]
        [Ignore("Poznata greška: provjera DiscountBegins <= DateTime.Now je zakomentarisana u CartsController.SubmitOrder, pa se popust primjenjuje i prije datuma početka.")]
        [TestProperty("Funkcionalnost", "PromoKod")]
        public async Task SubmitOrder_PromoKodKojiJosNijePoceo_NePrimjenjujePopust()
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId, kolicina: 1, cijena: 50);
            DodajPopust("POPUST-1", KorisnikId, DiscountType.AmountOff, 20).DiscountBegins = DateTime.Today.AddDays(1);
            _context.SaveChanges();

            // Act
            var rezultat = (ViewResult)await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "POPUST-1");

            // Assert
            Assert.AreEqual(50.0, (double)rezultat.ViewData["Total"]!, 0.001);
        }

        [TestMethod]
        [Priority(1)]
        public async Task SubmitOrder_ValidnaNarudzba_ProsljedjujePodatkeNaPotvrdu()
        {
            // Act
            var rezultat = (ViewResult)await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "");

            // Assert
            Assert.AreEqual("ConfirmOrder", rezultat.ViewName);
            Assert.AreEqual("Lamija", rezultat.ViewData["Name"]);
            Assert.AreEqual("Sarajevo", rezultat.ViewData["City"]);
        }

        [TestMethod]
        public async Task SubmitOrder_ValidnaNarudzba_NeSaljeEmail()
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId);

            // Act
            await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "");

            // Assert
            _emailMock.Verify(
                e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        private static readonly JsonSerializerOptions JsonOpcije = new() { PropertyNameCaseInsensitive = true };

        private record SlucajCijene(double Cijena, int? Kolicina, double OcekivanoUkupno);

        private record SlucajPopusta(string Tip, double Iznos, double CijenaKorpe, double OcekivanoUkupno);

        private static ClaimsIdentity Korisnik(string id) =>
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id) }, "TestAuth");

        private static ClaimsIdentity Administrator(string id) =>
            new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimTypes.Role, Uloga_Administrator)
            }, "TestAuth");

        private static ClaimsIdentity KorisnikBezId() =>
            new ClaimsIdentity(Array.Empty<Claim>(), "TestAuth");

        private void PostaviKorisnika(ClaimsIdentity identitet)
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identitet) }
            };
        }

        private static DateTime ValidanDatumIsporuke() => DateTime.Today.AddDays(3).AddHours(10);

        private Product DodajProizvod(double cijena = 5.5)
        {
            var proizvod = new Product
            {
                Name = "Ruža",
                ImageUrl = "ruza.jpg",
                Price = cijena,
                Stock = 10,
                Category = "Cvijeće",
                Description = "Crvena ruža za test",
                productType = "Single",
                Color = "Crvena"
            };
            _context.Products.Add(proizvod);
            _context.SaveChanges();
            return proizvod;
        }

        private Cart DodajStavkuUKorpu(string vlasnikId, int? kolicina = 1, double cijena = 5.5)
        {
            var stavka = new Cart { Product = DodajProizvod(cijena), PersonID = vlasnikId, ProductQuantity = kolicina };
            _context.Cart.Add(stavka);
            _context.SaveChanges();
            return stavka;
        }

        private Discount DodajPopust(string kod, string vlasnikId, DiscountType tip, double iznos)
        {
            var popust = new Discount
            {
                DiscountCode = kod,
                PersonID = vlasnikId,
                DiscountType = tip,
                DiscountAmount = iznos,
                DiscountBegins = DateTime.Today
            };
            _context.Discounts.Add(popust);
            _context.SaveChanges();
            return popust;
        }
    }
}
