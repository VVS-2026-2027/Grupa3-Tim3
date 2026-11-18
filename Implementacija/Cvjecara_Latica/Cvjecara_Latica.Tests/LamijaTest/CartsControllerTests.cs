using System.Security.Claims;
using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Cvjecara_Latica.Services;
using Cvjecara_Latica.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cvjecara_Latica.Tests.LamijaTest
{
    [TestClass]
    public class CartsControllerTests
    {
        private const string KorisnikId = "korisnik-1";
        private const string DrugiKorisnikId = "korisnik-2";

        private ApplicationDbContext _context = null!;
        private Mock<IEmailService> _emailMock = null!;
        private CartsController _controller = null!;

        [TestInitialize]
        public void Postavi()
        {
            _context = TestDbContextFactory.Kreiraj();          
            _emailMock = new Mock<IEmailService>();           
            _controller = new CartsController(_context, _emailMock.Object);
            PrijaviKorisnika(KorisnikId);
        }

        [TestCleanup]
        public void Ocisti()
        {
            _controller.Dispose();
            _context.Dispose();
        }


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


        // Postavlja lažnog prijavljenog korisnika (kontroler čita User iz HttpContext-a)
        private void PrijaviKorisnika(string id, string? uloga = null)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, id) };
            if (uloga != null)
                claims.Add(new Claim(ClaimTypes.Role, uloga));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
        }

        // Dodaje jednu stavku (ruža) u korpu zadanog korisnika i vraća je
        private Cart DodajStavkuUKorpu(string vlasnikId, int kolicina = 1)
        {
            var proizvod = new Product
            {
                Name = "Ruža",
                ImageUrl = "ruza.jpg",
                Price = 5.5,
                Stock = 10,
                Category = "Cvijeće",
                Description = "Crvena ruža za test",
                productType = "Single",
                Color = "Crvena"
            };
            var stavka = new Cart { Product = proizvod, PersonID = vlasnikId, ProductQuantity = kolicina };
            _context.Cart.Add(stavka);
            _context.SaveChanges();
            return stavka;
        }
    }
}
