using Cvjecara_Latica.Models;
using Cvjecara_Latica.Services;

namespace Cvjecara_Latica.Tests.TDD
{
    [TestClass]
    [TestCategory("TDD-F2-Zalihe")]
    public class ZaliheServiceTests
    {
        private ZaliheService _servis = null!;

        [TestInitialize]
        public void Postavi()
        {
            _servis = new ZaliheService();
        }
        // Pomoćna metoda za kreiranje stavke korpe (bez logike, samo gradi objekat)
        private static Cart Stavka(int proizvodId, int zaliha, int? kolicina) =>
            new Cart
            {
                ProductID = proizvodId,
                ProductQuantity = kolicina,
                Product = new Product { ProductID = proizvodId, Stock = zaliha }
            };
        [TestMethod]
        public void ProizvodiBezDovoljnoZaliha_StavkeNull_BacaArgumentNullException()
        {
            // Arrange: servis je kreiran u metodi Postavi; ulaz je null.

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(
                () => _servis.ProizvodiBezDovoljnoZaliha(null!));
        }
        // ---------- Ciklus 2: nema nedostajućih proizvoda ----------

        [TestMethod]
        public void ProizvodiBezDovoljnoZaliha_PraznaKorpa_VracaPraznuListu()
        {
            // Act
            var rezultat = _servis.ProizvodiBezDovoljnoZaliha(new List<Cart>());

            // Assert
            Assert.AreEqual(0, rezultat.Count);
        }

        [TestMethod]
        public void ProizvodiBezDovoljnoZaliha_SveKolicineDostupne_VracaPraznuListu()
        {
            // Arrange
            var stavke = new List<Cart> { Stavka(1, 10, 3), Stavka(2, 5, 5) };

            // Act
            var rezultat = _servis.ProizvodiBezDovoljnoZaliha(stavke);

            // Assert
            Assert.AreEqual(0, rezultat.Count);
        }

        // ---------- Ciklus 3: nedovoljne zalihe ----------

        [TestMethod]
        public void ProizvodiBezDovoljnoZaliha_KolicinaVecaOdZalihe_VracaIdProizvoda()
        {
            // Arrange
            var stavke = new List<Cart>
            {
                Stavka(1, 10, 3),
                Stavka(2, 2, 5),
                Stavka(3, 0, 1)
            };
            var ocekivano = new List<int> { 2, 3 };

            // Act
            var rezultat = _servis.ProizvodiBezDovoljnoZaliha(stavke);

            // Assert
            CollectionAssert.AreEqual(ocekivano, rezultat);
        }
        // ---------- Ciklus 4: neispravna količina ----------

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(null)]
        public void ProizvodiBezDovoljnoZaliha_NeispravnaKolicina_BacaArgumentException(int? kolicina)
        {
            // Arrange
            var stavke = new List<Cart> { Stavka(1, 10, kolicina) };

            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(
                () => _servis.ProizvodiBezDovoljnoZaliha(stavke));
        }
    }
}
