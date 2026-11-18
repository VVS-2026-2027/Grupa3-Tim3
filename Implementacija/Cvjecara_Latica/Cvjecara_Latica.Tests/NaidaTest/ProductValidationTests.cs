
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cvjecara_Latica.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cvjecara_Latica.Tests.NaidaTest
{
    [TestClass]
    public class ProductValidationTests
    {
        private Product NapraviProizvod()
        {
            return new Product
            {
                Name = "Ruza",
                ImageUrl = "/images/ruza.jpg",
                Price = 15.0,
                FlowerType = "Rose",
                Stock = 20,
                Category = "Cvijece",
                Description = "Prelijepa crvena ruza",
                productType = "Flower",
                Color = "Red"
            };
        }

        private bool Validan(Product proizvod)
        {
            var rezultati = new List<ValidationResult>();
            var kontekst = new ValidationContext(proizvod);

            return Validator.TryValidateObject(
                proizvod,
                kontekst,
                rezultati,
                true
            );
        }

        [TestMethod]
        public void Product_IspravniPodaci_Validan()
        {
            var proizvod = NapraviProizvod();

            Assert.IsTrue(Validan(proizvod));
        }

        [TestMethod]
        public void Product_PrazanNaziv_NijeValidan()
        {
            var proizvod = NapraviProizvod();
            proizvod.Name = "";

            Assert.IsFalse(Validan(proizvod));
        }

        [TestMethod]
        public void Product_NazivJedanZnak_NijeValidan()
        {
            var proizvod = NapraviProizvod();
            proizvod.Name = "R";

            Assert.IsFalse(Validan(proizvod));
        }

        [TestMethod]
        public void Product_NegativnaCijena_NijeValidan()
        {
            var proizvod = NapraviProizvod();
            proizvod.Price = -5;

            Assert.IsFalse(Validan(proizvod));
        }

        [TestMethod]
        public void Product_CijenaNula_NijeValidan()
        {
            var proizvod = NapraviProizvod();
            proizvod.Price = 0;

            Assert.IsFalse(Validan(proizvod));
        }

        [TestMethod]
        public void Product_CijenaPrekoMaksimuma_NijeValidan()
        {
            var proizvod = NapraviProizvod();
            proizvod.Price = 10000.01;

            Assert.IsFalse(Validan(proizvod));
        }

        [TestMethod]
        public void Product_NegativneZalihe_NijeValidan()
        {
            var proizvod = NapraviProizvod();
            proizvod.Stock = -1;

            Assert.IsFalse(Validan(proizvod));
        }

        [TestMethod]
        public void Product_ZalihePrekoMaksimuma_NijeValidan()
        {
            var proizvod = NapraviProizvod();
            proizvod.Stock = 1001;

            Assert.IsFalse(Validan(proizvod));
        }

        [TestMethod]
        public void Product_KratakOpis_NijeValidan()
        {
            var proizvod = NapraviProizvod();
            proizvod.Description = "Kratko";

            Assert.IsFalse(Validan(proizvod));
        }

        [TestMethod]
        public void Product_NeispravanTipCvijeta_NijeValidan()
        {
            var proizvod = NapraviProizvod();
            proizvod.FlowerType = "Rose123";

            Assert.IsFalse(Validan(proizvod));
        }
    }
}
