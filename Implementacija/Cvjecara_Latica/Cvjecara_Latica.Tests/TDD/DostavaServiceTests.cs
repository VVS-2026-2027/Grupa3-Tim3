using Cvjecara_Latica.Services;

namespace Cvjecara_Latica.Tests.TDD
{
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


        [TestMethod]
        [DataRow(0.0)]
        [DataRow(-0.01)]
        [DataRow(-50.0)]
        public void IzracunajCijenuDostave_IznosNijePozitivan_BacaArgumentOutOfRangeException(double iznos)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => _servis.IzracunajCijenuDostave(iznos, "Sarajevo"));
        }

       

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
    }
}