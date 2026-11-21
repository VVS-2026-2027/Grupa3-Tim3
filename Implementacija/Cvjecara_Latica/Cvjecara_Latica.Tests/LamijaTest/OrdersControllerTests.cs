using System.Security.Claims;
using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Cvjecara_Latica.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cvjecara_Latica.Tests.LamijaTest
{
    [TestClass]
    [TestCategory("OrdersController")]
    public class OrdersControllerTests
    {
        private const string KorisnikId = "korisnik-1";
        private const string DrugiKorisnikId = "korisnik-2";
        private const string AdminId = "admin-1";
        private const string Uloga_Administrator = "Administrator";

        private ApplicationDbContext _context = null!;
        private OrdersController _controller = null!;

        [TestInitialize]
        public void Postavi()
        {
            _context = TestDbContextFactory.Kreiraj();
            _controller = new OrdersController(_context);
            PrijaviKorisnika(AdminId, Uloga_Administrator);
        }

        [TestCleanup]
        public void Ocisti()
        {
            _controller.Dispose();
            _context.Dispose();
        }

        [TestMethod]
        [Priority(1)]
        public async Task Index_Administrator_VidiSveNarudzbe()
        {
            // Arrange
            DodajNarudzbu(KorisnikId);
            DodajNarudzbu(DrugiKorisnikId);

            // Act
            var model = (List<Order>)((ViewResult)await _controller.Index()).Model!;

            // Assert
            Assert.HasCount(2, model);
        }

        [TestMethod]
        public async Task Index_ObicanKorisnik_VidiSamoSvojeNarudzbe()
        {
            // Arrange
            var moja = DodajNarudzbu(KorisnikId);
            DodajNarudzbu(DrugiKorisnikId);
            PrijaviKorisnika(KorisnikId);

            // Act
            var model = (List<Order>)((ViewResult)await _controller.Index()).Model!;

            // Assert
            CollectionAssert.AreEqual(new[] { moja.OrderID }, model.Select(o => o.OrderID).ToArray());
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
        public async Task Details_NepostojeciId_VracaNotFound()
        {
            // Act
            var rezultat = await _controller.Details(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_PostojeciId_VracaViewSaNarudzbomIPlacanjem()
        {
            // Arrange
            var narudzba = DodajNarudzbu(KorisnikId);

            // Act
            var model = (Order)((ViewResult)await _controller.Details(narudzba.OrderID)).Model!;

            // Assert
            Assert.AreEqual(narudzba.OrderID, model.OrderID);
            Assert.IsNotNull(model.Payment);
        }

        [TestMethod]
        public void Create_Get_VracaViewSaListamaZaOdabir()
        {
            // Act
            var rezultat = (ViewResult)_controller.Create();

            // Assert
            Assert.IsInstanceOfType<SelectList>(rezultat.ViewData["PaymentID"]);
            Assert.IsInstanceOfType<SelectList>(rezultat.ViewData["PersonID"]);
        }

        [TestMethod]
        [Priority(1)]
        public async Task Create_Post_ValidnaNarudzba_SpremaIPreusmjeravaNaIndex()
        {
            // Arrange
            var nova = NovaNarudzba(KorisnikId, DodajPlacanje().PaymentID);

            // Act
            var rezultat = (RedirectToActionResult)await _controller.Create(nova);

            // Assert
            Assert.AreEqual(nameof(OrdersController.Index), rezultat.ActionName);
            Assert.AreEqual(1, _context.Orders.Count());
        }

        [TestMethod]
        public async Task Create_Post_NevalidanModel_VracaViewINeSpasava()
        {
            // Arrange
            var nova = NovaNarudzba(KorisnikId, DodajPlacanje().PaymentID);
            _controller.ModelState.AddModelError("TotalAmountToPay", "Total amount is required.");

            // Act
            var rezultat = (ViewResult)await _controller.Create(nova);

            // Assert
            Assert.AreSame(nova, rezultat.Model);
            Assert.AreEqual(0, _context.Orders.Count());
        }

        [TestMethod]
        public async Task Edit_Get_IdJeNull_VracaNotFound()
        {
            // Act
            var rezultat = await _controller.Edit(null);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Edit_Get_NepostojeciId_VracaNotFound()
        {
            // Act
            var rezultat = await _controller.Edit(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Edit_Get_PostojeciId_VracaViewSaNarudzbomIListama()
        {
            // Arrange
            var narudzba = DodajNarudzbu(KorisnikId);

            // Act
            var rezultat = (ViewResult)await _controller.Edit(narudzba.OrderID);

            // Assert
            Assert.AreEqual(narudzba.OrderID, ((Order)rezultat.Model!).OrderID);
            Assert.IsInstanceOfType<SelectList>(rezultat.ViewData["PaymentID"]);
        }

        [TestMethod]
        public async Task Edit_Post_IdSeNePoklapa_VracaNotFound()
        {
            // Arrange
            var izmjena = NovaNarudzba(KorisnikId, 1);
            izmjena.OrderID = 5;

            // Act
            var rezultat = await _controller.Edit(6, izmjena);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Edit_Post_ValidnaIzmjena_MijenjaNarudzbuIPreusmjerava()
        {
            // Arrange
            var narudzba = DodajNarudzbu(KorisnikId);
            var izmjena = NovaNarudzba(KorisnikId, narudzba.PaymentID);
            izmjena.OrderID = narudzba.OrderID;
            izmjena.TotalAmountToPay = 75;

            // Act
            var rezultat = (RedirectToActionResult)await _controller.Edit(narudzba.OrderID, izmjena);

            // Assert
            Assert.AreEqual(nameof(OrdersController.Index), rezultat.ActionName);
            Assert.AreEqual(75.0, _context.Orders.Single().TotalAmountToPay, 0.001);
        }

        [TestMethod]
        public async Task Edit_Post_NevalidanModel_VracaViewSaIstimModelom()
        {
            // Arrange
            var narudzba = DodajNarudzbu(KorisnikId);
            var izmjena = NovaNarudzba(KorisnikId, narudzba.PaymentID);
            izmjena.OrderID = narudzba.OrderID;
            _controller.ModelState.AddModelError("Rating", "Rating must be between 0 and 5.");

            // Act
            var rezultat = (ViewResult)await _controller.Edit(narudzba.OrderID, izmjena);

            // Assert
            Assert.AreSame(izmjena, rezultat.Model);
            Assert.IsInstanceOfType<SelectList>(rezultat.ViewData["PersonID"]);
        }

        [TestMethod]
        public async Task Edit_Post_NarudzbaObrisanaUMedjuvremenu_VracaNotFound()
        {
            // Arrange
            var izmjena = NovaNarudzba(KorisnikId, DodajPlacanje().PaymentID);
            izmjena.OrderID = 999;

            // Act
            var rezultat = await _controller.Edit(999, izmjena);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Edit_Post_KonfliktINarudzbaPostoji_PonovoBacaIzuzetak()
        {
            // Arrange
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var bazaSaKonfliktom = new Mock<ApplicationDbContext>(opcije) { CallBase = true };
            bazaSaKonfliktom
                .Setup(b => b.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException());
            var narudzba = DodajNarudzbuU(bazaSaKonfliktom.Object, KorisnikId);
            using var kontroler = new OrdersController(bazaSaKonfliktom.Object);

            // Act & Assert
            await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(
                () => kontroler.Edit(narudzba.OrderID, narudzba));
            bazaSaKonfliktom.Verify(b => b.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Delete_Get_IdJeNull_VracaNotFound()
        {
            // Act
            var rezultat = await _controller.Delete(null);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Delete_Get_NepostojeciId_VracaNotFound()
        {
            // Act
            var rezultat = await _controller.Delete(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Delete_Get_PostojeciId_VracaViewZaPotvrdu()
        {
            // Arrange
            var narudzba = DodajNarudzbu(KorisnikId);

            // Act
            var model = (Order)((ViewResult)await _controller.Delete(narudzba.OrderID)).Model!;

            // Assert
            Assert.AreEqual(narudzba.OrderID, model.OrderID);
        }

        [TestMethod]
        public async Task DeleteConfirmed_PostojeciId_BriseNarudzbuIPreusmjerava()
        {
            // Arrange
            var narudzba = DodajNarudzbu(KorisnikId);

            // Act
            var rezultat = (RedirectToActionResult)await _controller.DeleteConfirmed(narudzba.OrderID);

            // Assert
            Assert.AreEqual(nameof(OrdersController.Index), rezultat.ActionName);
            Assert.AreEqual(0, _context.Orders.Count());
        }

        [TestMethod]
        public async Task DeleteConfirmed_NepostojeciId_PreusmjeravaINeBriseDrugeNarudzbe()
        {
            // Arrange
            DodajNarudzbu(KorisnikId);

            // Act
            var rezultat = (RedirectToActionResult)await _controller.DeleteConfirmed(999);

            // Assert
            Assert.AreEqual(nameof(OrdersController.Index), rezultat.ActionName);
            Assert.AreEqual(1, _context.Orders.Count());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(999)]
        public async Task MarkAsPickedUp_NepostojeciId_VracaNotFound(int id)
        {
            // Act
            var rezultat = await _controller.MarkAsPickedUp(id);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        [Priority(1)]
        public async Task MarkAsPickedUp_PostojecaNarudzba_OznacavaKaoPreuzetuIPreusmjerava()
        {
            // Arrange
            var narudzba = DodajNarudzbu(KorisnikId);

            // Act
            var rezultat = (RedirectToActionResult)await _controller.MarkAsPickedUp(narudzba.OrderID);

            // Assert
            Assert.AreEqual("Index", rezultat.ActionName);
            Assert.IsTrue(_context.Orders.Single().IsPickedUp);
        }

        private void PrijaviKorisnika(string id, string? uloga = null)
        {
            var tvrdnje = new List<Claim> { new(ClaimTypes.NameIdentifier, id) };
            if (uloga != null) tvrdnje.Add(new Claim(ClaimTypes.Role, uloga));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(tvrdnje, "Test")) }
            };
        }

        private Payment DodajPlacanje() => DodajPlacanjeU(_context);

        private static Payment DodajPlacanjeU(ApplicationDbContext baza)
        {
            var placanje = new Payment { PaymentType = PaymentType.Cash, PayedAmount = 50, DeliveryAddress = "Adresa 1" };
            baza.Payments.Add(placanje);
            baza.SaveChanges();
            return placanje;
        }

        private Order DodajNarudzbu(string vlasnikId) => DodajNarudzbuU(_context, vlasnikId);

        private static Order DodajNarudzbuU(ApplicationDbContext baza, string vlasnikId)
        {
            if (baza.Users.Find(vlasnikId) == null)
                baza.Users.Add(new Person { Id = vlasnikId, UserName = vlasnikId, FirstName = "Ime", LastName = "Prezime", HomeAdress = "Adresa 1" });
            var narudzba = NovaNarudzba(vlasnikId, DodajPlacanjeU(baza).PaymentID);
            baza.Orders.Add(narudzba);
            baza.SaveChanges();
            baza.ChangeTracker.Clear();
            return narudzba;
        }

        private static Order NovaNarudzba(string vlasnikId, int placanjeId) => new()
        {
            PersonID = vlasnikId,
            PaymentID = placanjeId,
            purchaseDate = DateTime.Today,
            DeliveryDate = DateTime.Today.AddDays(3),
            TotalAmountToPay = 50
        };
    }
}
