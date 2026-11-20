using Cvjecara_Latica.Services;

namespace Cvjecara_Latica.Tests.TDD
{
    /// <summary>
    /// TDD testovi za funkcionalnost F1 – cijena dostave.
    /// </summary>
    [TestClass]
    [TestCategory("TDD-F1-Dostava")]
    public class DostavaServiceTests
    {
        private DostavaService _servis = null!;

        [TestInitialize]
        public void Postavi()
        {
            _servis = new DostavaService();
        }

        // ---------- Ciklus 1: neispravan iznos ----------
        [TestMethod]
        [DataRow(0.0)]
        [DataRow(-0.01)]
        [DataRow(-50.0)]
        public void IzracunajCijenuDostave_IznosNijePozitivan_BacaArgumentOutOfRangeException(double iznos)
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => _servis.IzracunajCijenuDostave(iznos, "Sarajevo"));
        }

        // ---------- Ciklus 2: grad nije unesen ----------
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void IzracunajCijenuDostave_GradNijeUnesen_BacaArgumentException(string? grad)
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(
                () => _servis.IzracunajCijenuDostave(50.0, grad!));
        }

        // ---------- Ciklus 3: cijena po gradu ----------
        [TestMethod]
        [DataRow("Sarajevo", 5.0)]
        [DataRow("Mostar", 10.0)]
        [DataRow("Tuzla", 10.0)]
        public void IzracunajCijenuDostave_IznosIspodPraga_VracaCijenuPremaGradu(string grad, double ocekivanaCijena)
        {
            // Arrange
            double iznos = 50.0;

            // Act
            double cijena = _servis.IzracunajCijenuDostave(iznos, grad);

            // Assert
            Assert.AreEqual(ocekivanaCijena, cijena, 0.001);
        }

        // ---------- Ciklus 4: besplatna dostava ----------
        [TestMethod]
        [DataRow(100.0, "Sarajevo")]
        [DataRow(100.0, "Mostar")]
        [DataRow(250.0, "Bihać")]
        public void IzracunajCijenuDostave_IznosOd100IVise_DostavaBesplatna(double iznos, string grad)
        {
            // Act
            double cijena = _servis.IzracunajCijenuDostave(iznos, grad);

            // Assert
            Assert.AreEqual(0.0, cijena, 0.001);
        }

        [TestMethod]
        [DataRow("Sarajevo", 5.0)]
        [DataRow("Mostar", 10.0)]
        public void IzracunajCijenuDostave_IznosTikIspodPraga_DostavaSeNaplacuje(string grad, double ocekivanaCijena)
        {
            // Act
            double cijena = _servis.IzracunajCijenuDostave(99.99, grad);

            // Assert
            Assert.AreEqual(ocekivanaCijena, cijena, 0.001);
        }

        // ---------- Ciklus 5: format naziva grada ----------
        [TestMethod]
        [DataRow("sarajevo")]
        [DataRow("SARAJEVO")]
        [DataRow("  Sarajevo  ")]
        public void IzracunajCijenuDostave_GradRazlicitoNapisan_PrepoznajeSarajevo(string grad)
        {
            // Act
            double cijena = _servis.IzracunajCijenuDostave(50.0, grad);

            // Assert
            Assert.AreEqual(5.0, cijena, 0.001);
        }
    }
}