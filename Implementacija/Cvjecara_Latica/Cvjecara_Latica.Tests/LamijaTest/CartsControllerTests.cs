using System.Security.Claims;
using System.Text.Json;
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
    /// <summary>
    /// Unit testovi za CartsController – I dio (Clan2):
    /// Index, Details, Create, Edit, Delete, AddToCart, PlaceOrder, SubmitOrder.
    /// Baza je zamijenjena InMemory bazom, a IEmailService i ITempDataProvider Moq zamjenskim objektima.
    /// </summary>
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

        // ==================== Inicijalizacija i čišćenje ====================

        [ClassInitialize]
        public static void ProvjeriTestnePodatke(TestContext context)
        {
            // Izvršava se jednom prije svih testova u klasi – provjera da su fajlovi s podacima kopirani
            Assert.IsTrue(File.Exists(PutanjaPodataka + "dodavanje_u_korpu.csv"));
            Assert.IsTrue(File.Exists(PutanjaPodataka + "datumi_isporuke.csv"));
            Assert.IsTrue(File.Exists(PutanjaPodataka + "ukupna_cijena.json"));
            Assert.IsTrue(File.Exists(PutanjaPodataka + "popusti.json"));
        }

        [TestInitialize]
        public void Postavi()
        {
            _context = TestDbContextFactory.Kreiraj();      // nova prazna baza za svaki test

            _emailMock = new Mock<IEmailService>();         // zamjenski objekat – mail se ne šalje stvarno

            _tempDataMock = new Mock<ITempDataProvider>();  // zamjenski objekat – TempData bez stvarnih sesija/cookieja
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

        // ==================== Index ====================

        [TestMethod]
        public async Task Index_PrijavljenKorisnik_VracaSamoNjegoveStavke()
        {
            // Arrange – ulaz: 2 stavke prijavljenog korisnika i 1 stavka drugog korisnika
            DodajStavkuUKorpu(KorisnikId);
            DodajStavkuUKorpu(KorisnikId);
            DodajStavkuUKorpu(DrugiKorisnikId);

            // Act
            var rezultat = await _controller.Index();

            // Assert – očekivani izlaz: View sa samo 2 stavke
            var model = (List<Cart>)((ViewResult)rezultat).Model!;
            Assert.HasCount(2, model);
        }

        [TestMethod]
        public async Task Index_NeprijavljenKorisnik_VracaStavkeAnonimnogKorisnika()
        {
            // Arrange – ulaz: korisnik nije prijavljen, u korpi je stavka "anonimni"
            PostaviKorisnika(new ClaimsIdentity());
            DodajStavkuUKorpu("anonimni");

            // Act
            var rezultat = await _controller.Index();

            // Assert – očekivani izlaz: View sa 1 stavkom anonimnog korisnika
            var model = (List<Cart>)((ViewResult)rezultat).Model!;
            Assert.HasCount(1, model);
        }

        [TestMethod]
        public async Task Index_PrijavljenKorisnikBezId_BacaNullReferenceException()
        {
            // Arrange – ulaz: prijavljen korisnik koji nema NameIdentifier claim
            PostaviKorisnika(KorisnikBezId());

            // Act & Assert – očekivani izlaz: izuzetak (kontroler ne provjerava null)
            await Assert.ThrowsExactlyAsync<NullReferenceException>(() => _controller.Index());
        }

        // ==================== Details ====================

        [TestMethod]
        public async Task Details_IdJeNull_VracaNotFound()
        {
            // Act – ulaz: id = null
            var rezultat = await _controller.Details(null);

            // Assert – očekivani izlaz: 404
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_StavkaPrijavljenogKorisnika_VracaViewSaTomStavkom()
        {
            // Arrange – ulaz: stavka u korpi koja pripada prijavljenom korisniku
            var stavka = DodajStavkuUKorpu(KorisnikId);

            // Act
            var rezultat = await _controller.Details(stavka.CartID);

            // Assert – očekivani izlaz: View čiji je model baš ta stavka
            var view = rezultat as ViewResult;
            Assert.IsNotNull(view);
            Assert.AreEqual(stavka.CartID, ((Cart)view.Model!).CartID);
        }

        [TestMethod]
        public async Task Details_StavkaDrugogKorisnika_VracaForbid()
        {
            // Arrange – ulaz: stavka koja pripada nekom drugom korisniku
            var tudjaStavka = DodajStavkuUKorpu(DrugiKorisnikId);

            // Act
            var rezultat = await _controller.Details(tudjaStavka.CartID);

            // Assert – očekivani izlaz: zabranjen pristup
            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_AdministratorTudjaStavka_VracaView()
        {
            // Arrange – ulaz: administrator otvara stavku drugog korisnika
            var tudjaStavka = DodajStavkuUKorpu(DrugiKorisnikId);
            PostaviKorisnika(Administrator(AdminId));

            // Act
            var rezultat = await _controller.Details(tudjaStavka.CartID);

            // Assert – očekivani izlaz: administrator smije vidjeti stavku
            Assert.IsInstanceOfType<ViewResult>(rezultat);
        }

        // ==================== Create ====================

        [TestMethod]
        public void Create_Get_VracaViewSaListamaZaOdabir()
        {
            // Act
            var rezultat = _controller.Create();

            // Assert – očekivani izlaz: View i popunjene padajuće liste proizvoda i osoba
            Assert.IsInstanceOfType<ViewResult>(rezultat);
            Assert.IsInstanceOfType<SelectList>(_controller.ViewData["ProductID"]);
            Assert.IsInstanceOfType<SelectList>(_controller.ViewData["PersonID"]);
        }

        [TestMethod]
        public async Task Create_ValidnaStavka_DodajeStavkuIPreusmjeravaNaIndex()
        {
            // Arrange – ulaz: ispravna nova stavka
            var proizvod = DodajProizvod();
            var novaStavka = new Cart { ProductID = proizvod.ProductID, PersonID = KorisnikId, ProductQuantity = 2 };

            // Act
            var rezultat = await _controller.Create(novaStavka);

            // Assert – očekivani izlaz: stavka je u bazi i preusmjeravanje na Index
            Assert.AreEqual(1, _context.Cart.Count());
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        [TestMethod]
        public async Task Create_NevalidanModel_VracaIstiViewINeSpasava()
        {
            // Arrange – ulaz: model sa greškom validacije
            var novaStavka = new Cart { ProductID = 1, PersonID = KorisnikId, ProductQuantity = 2 };
            _controller.ModelState.AddModelError("ProductQuantity", "Neispravna količina");

            // Act
            var rezultat = await _controller.Create(novaStavka);

            // Assert – očekivani izlaz: vraća se isti model, baza je prazna
            Assert.AreSame(novaStavka, ((ViewResult)rezultat).Model);
            Assert.AreEqual(0, _context.Cart.Count());
        }

        // ==================== Edit (GET) ====================

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

        // ==================== Edit (POST) ====================

        [TestMethod]
        public async Task Edit_Post_IdSeNePoklapa_VracaNotFound()
        {
            // Arrange – ulaz: id iz rute (1) različit od CartID iz forme (2)
            var izmjena = new Cart { CartID = 2, ProductQuantity = 3 };

            // Act
            var rezultat = await _controller.Edit(1, izmjena);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Edit_Post_ValidnaIzmjena_MijenjaKolicinuIPreusmjerava()
        {
            // Arrange – ulaz: stavka s količinom 1, nova količina 5
            var stavka = DodajStavkuUKorpu(KorisnikId, kolicina: 1);
            var izmjena = new Cart { CartID = stavka.CartID, ProductQuantity = 5 };

            // Act
            var rezultat = await _controller.Edit(stavka.CartID, izmjena);

            // Assert – očekivani izlaz: količina 5 u bazi i preusmjeravanje na Index
            Assert.AreEqual(5, _context.Cart.Single().ProductQuantity);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
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

            // Assert – očekivani izlaz: isti model nazad, količina u bazi nepromijenjena
            Assert.AreSame(izmjena, ((ViewResult)rezultat).Model);
            Assert.AreEqual(1, _context.Cart.Single().ProductQuantity);
        }

        [TestMethod]
        public async Task Edit_Post_AdministratorMijenjaDruguStavku_MijenjaSeSamoTaStavka()
        {
            // Arrange – ulaz: dvije tuđe stavke, administrator mijenja DRUGU
            // (ovaj test je otkrio grešku s prioritetom && i || u upitu – ispravljeno u CartsController.Edit)
            var prvaStavka = DodajStavkuUKorpu(DrugiKorisnikId, kolicina: 1);
            var drugaStavka = DodajStavkuUKorpu(DrugiKorisnikId, kolicina: 1);
            PostaviKorisnika(Administrator(AdminId));
            var izmjena = new Cart { CartID = drugaStavka.CartID, ProductQuantity = 7 };

            // Act
            await _controller.Edit(drugaStavka.CartID, izmjena);

            // Assert – očekivani izlaz: promijenjena je druga stavka, prva je ostala ista
            Assert.AreEqual(7, drugaStavka.ProductQuantity);
            Assert.AreEqual(1, prvaStavka.ProductQuantity);
        }

        // ==================== Delete ====================

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

            // Assert – očekivani izlaz: korpa je prazna, preusmjeravanje na Index
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

        // ==================== AddToCart ====================

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(999)]
        public void AddToCart_NepostojeciProizvod_VracaNotFound(int nepostojeciId)
        {
            // Act – ulaz (inline podaci): ID proizvoda koji ne postoji u bazi
            var rezultat = _controller.AddToCart(nepostojeciId);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public void AddToCart_NoviProizvod_DodajeStavkuSaKolicinomJedan()
        {
            // Arrange – ulaz: proizvod koji još nije u korpi
            var proizvod = DodajProizvod();

            // Act
            _controller.AddToCart(proizvod.ProductID);

            // Assert – očekivani izlaz: nova stavka korisnika s količinom 1
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

            // Assert – očekivani izlaz: Products/Details/{id}
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

        // Data-driven (CSV): početna količina u korpi -> očekivana količina nakon ponovnog dodavanja
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
            // Arrange – ulaz iz CSV-a: stavka s početnom količinom (može biti i null)
            var stavka = DodajStavkuUKorpu(KorisnikId, pocetnaKolicina);

            // Act
            _controller.AddToCart(stavka.ProductID);

            // Assert – očekivani izlaz iz CSV-a
            Assert.AreEqual(ocekivanaKolicina, _context.Cart.Single().ProductQuantity);
        }

        // ==================== PlaceOrder ====================

        // Data-driven (JSON): cijena i količina stavke -> očekivana ukupna cijena
        public static IEnumerable<object?[]> UkupneCijeneIzJson()
        {
            var slucajevi = JsonSerializer.Deserialize<List<SlucajCijene>>(
                File.ReadAllText(PutanjaPodataka + "ukupna_cijena.json"), JsonOpcije)!;
            return slucajevi.Select(s => new object?[] { s.Cijena, s.Kolicina, s.OcekivanoUkupno });
        }

        [TestMethod]
        [DynamicData(nameof(UkupneCijeneIzJson))]
        public void PlaceOrder_StavkaUKorpi_RacunaUkupnuCijenu(double cijena, int? kolicina, double ocekivanoUkupno)
        {
            // Arrange – ulaz iz JSON-a
            DodajStavkuUKorpu(KorisnikId, kolicina, cijena);

            // Act
            var rezultat = (ViewResult)_controller.PlaceOrder();

            // Assert – poređenje decimalnih brojeva s tolerancijom (ne ==)
            Assert.AreEqual(ocekivanoUkupno, (double)rezultat.ViewData["Total"]!, 0.001);
        }

        [TestMethod]
        public void PlaceOrder_NeprijavljenKorisnik_UkupnaCijenaJeNula()
        {
            // Arrange – ulaz: korisnik bez ID-a, u bazi postoji tuđa stavka
            DodajStavkuUKorpu(DrugiKorisnikId);
            PostaviKorisnika(new ClaimsIdentity());

            // Act
            var rezultat = (ViewResult)_controller.PlaceOrder();

            // Assert
            Assert.AreEqual(0.0, (double)rezultat.ViewData["Total"]!, 0.001);
        }

        // ==================== SubmitOrder ====================

        // Data-driven (CSV): datum isporuke (dana od danas + sat) -> koji se pogled vraća
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
            // Arrange – ulaz iz CSV-a: dozvoljeno je najranije za 2 dana, između 09:00 i 17:00
            DodajStavkuUKorpu(KorisnikId);
            var datumIsporuke = DateTime.Today.AddDays(danaOdDanas).AddHours(sat);

            // Act
            var rezultat = await _controller.SubmitOrder("Lamija", "Sarajevo", "Zmaja od Bosne 1", "061111222", datumIsporuke, "");

            // Assert – ConfirmOrder (validan datum) ili ponovo PlaceOrder (nevalidan datum)
            Assert.AreEqual(ocekivaniPogled, ((ViewResult)rezultat).ViewName);
        }

        [TestMethod]
        public async Task SubmitOrder_NevalidanDatum_DodajeGreskuUModelState()
        {
            // Act – ulaz: isporuka danas (premalo vremena)
            await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", DateTime.Today.AddHours(10), "");

            // Assert
            Assert.IsTrue(_controller.ModelState.ContainsKey("deliveryDate"));
        }

        // Data-driven (JSON): vrsta i iznos popusta -> očekivana cijena nakon popusta
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
        [Timeout(2000, CooperativeCancellation = true)]
        [DynamicData(nameof(PopustiIzJson))]
        public async Task SubmitOrder_ValidanPromoKod_UmanjujeCijenu(DiscountType tip, double iznos, double cijenaKorpe, double ocekivanoUkupno)
        {
            // Arrange – ulaz iz JSON-a: stavka zadane cijene i promo kod korisnika
            DodajStavkuUKorpu(KorisnikId, kolicina: 1, cijena: cijenaKorpe);
            DodajPopust("POPUST-1", KorisnikId, tip, iznos);

            // Act
            var rezultat = (ViewResult)await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "POPUST-1");

            // Assert – očekivani izlaz iz JSON-a (cijena ne može biti manja od 0)
            Assert.AreEqual(ocekivanoUkupno, (double)rezultat.ViewData["Total"]!, 0.001);
        }

        [TestMethod]
        public async Task SubmitOrder_ValidanPromoKod_OznacavaKodKaoIskoristen()
        {
            // Arrange
            DodajStavkuUKorpu(KorisnikId);
            var popust = DodajPopust("POPUST-1", KorisnikId, DiscountType.AmountOff, 1);

            // Act
            await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "POPUST-1");

            // Assert – kod je iskorišten i u TempData je zapisano da je popust primijenjen
            Assert.IsTrue(popust.IsUsed);
            Assert.IsTrue((bool)_controller.TempData["DiscountApplied"]!);
        }

        [TestMethod]
        public async Task SubmitOrder_NepostojeciPromoKod_CijenaOstajeIstaIPorukaOGresci()
        {
            // Arrange – ulaz: kod koji ne postoji
            DodajStavkuUKorpu(KorisnikId, kolicina: 2, cijena: 10);

            // Act
            var rezultat = (ViewResult)await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "NEPOSTOJI");

            // Assert
            Assert.AreEqual(20.0, (double)rezultat.ViewData["Total"]!, 0.001);
            Assert.IsFalse((bool)_controller.TempData["DiscountApplied"]!);
        }

        [TestMethod]
        public async Task SubmitOrder_PromoKodDrugogKorisnika_NePrimjenjujePopust()
        {
            // Arrange – ulaz: kod postoji, ali pripada drugom korisniku
            DodajStavkuUKorpu(KorisnikId, kolicina: 1, cijena: 50);
            var tudjiPopust = DodajPopust("POPUST-1", DrugiKorisnikId, DiscountType.AmountOff, 20);

            // Act
            var rezultat = (ViewResult)await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "POPUST-1");

            // Assert
            Assert.AreEqual(50.0, (double)rezultat.ViewData["Total"]!, 0.001);
            Assert.IsFalse(tudjiPopust.IsUsed);
        }

        [TestMethod]
        public async Task SubmitOrder_ValidnaNarudzba_ProsljedjujePodatkeNaPotvrdu()
        {
            // Act
            var rezultat = (ViewResult)await _controller.SubmitOrder("Lamija", "Sarajevo", "Adresa 1", "061111222", ValidanDatumIsporuke(), "");

            // Assert – podaci kupca su proslijeđeni pogledu ConfirmOrder
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

            // Assert – zamjenski objekat: mail se šalje tek u SubmitOrderFinal, ne ovdje
            _emailMock.Verify(
                e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        // ==================== Pomoćne metode i tipovi ====================

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

        // Postavlja lažnog korisnika (kontroler čita User iz HttpContext-a)
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
