using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Xml.Linq;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Cvjecara_Latica.Tests.Helpers;

namespace Cvjecara_Latica.Tests
{
    [TestClass]
    public class ProvjeraOkruzenjaTests
    {
        private ApplicationDbContext _context = null!;

        [TestInitialize]
        public void Postavi()
        {
            _context = TestDbContextFactory.Kreiraj();
        }

        [TestCleanup]
        public void Ocisti()
        {
            _context.Dispose();
        }

        [TestMethod]
        public void InMemoryBaza_DodanProizvod_ProizvodSeMozeProcitati()
        {
            var proizvod = NapraviProizvod("Ruža", 5.5);

            _context.Products.Add(proizvod);
            _context.SaveChanges();

            // Assert – očekivani izlaz: u bazi je tačno jedan proizvod s imenom "Ruža"
            Assert.AreEqual(1, _context.Products.Count());
            Assert.AreEqual("Ruža", _context.Products.Single().Name);
        }

        // ---------- Data-driven: izvori podataka iz fajlova ----------

        public static IEnumerable<object[]> ProizvodiIzCsv()
        {
            // Prvi red je zaglavlje: Naziv,Cijena,OcekivanoValidan
            foreach (var red in File.ReadAllLines("TestData/proizvodi_primjer.csv").Skip(1))
            {
                var kolone = red.Split(',');
                yield return new object[]
                {
                    kolone[0],
                    double.Parse(kolone[1], CultureInfo.InvariantCulture),
                    bool.Parse(kolone[2])
                };
            }
        }

        public static IEnumerable<object[]> ProizvodiIzXml()
        {
            var dokument = XDocument.Load("TestData/proizvodi_primjer.xml");
            foreach (var element in dokument.Descendants("proizvod"))
            {
                yield return new object[]
                {
                    (string)element.Attribute("naziv")!,
                    double.Parse((string)element.Attribute("cijena")!, CultureInfo.InvariantCulture),
                    bool.Parse((string)element.Attribute("ocekivanoValidan")!)
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(ProizvodiIzCsv))]
        [DynamicData(nameof(ProizvodiIzXml))]
        public void ValidacijaProizvoda_PodaciIzFajla_OdgovaraOcekivanom(string naziv, double cijena, bool ocekivanoValidan)
        {
            // Arrange – ulaz: naziv i cijena iz jednog reda fajla
            var proizvod = NapraviProizvod(naziv, cijena);
            var greske = new List<ValidationResult>();

            // Act – validacija modela prema atributima ([Required], [Range], [StringLength])
            bool jeValidan = Validator.TryValidateObject(proizvod, new ValidationContext(proizvod), greske, true);

            // Assert – očekivani izlaz: kolona OcekivanoValidan iz fajla
            Assert.AreEqual(ocekivanoValidan, jeValidan);
        }

        // Pomoćna metoda: proizvod s ispravnim vrijednostima svih ostalih polja
        private static Product NapraviProizvod(string naziv, double cijena)
        {
            return new Product
            {
                Name = naziv,
                Price = cijena,
                ImageUrl = "slika.jpg",
                Stock = 10,
                Category = "Cvijeće",
                Description = "Opis proizvoda za test",
                productType = "Single",
                Color = "Crvena"
            };
        }
    }
}
