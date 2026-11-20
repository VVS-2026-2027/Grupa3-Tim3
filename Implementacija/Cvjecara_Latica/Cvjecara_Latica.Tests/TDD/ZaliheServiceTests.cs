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

        [TestMethod]
        public void ProizvodiBezDovoljnoZaliha_StavkeNull_BacaArgumentNullException()
        {
            // Arrange: servis je kreiran u metodi Postavi; ulaz je null.

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(
                () => _servis.ProizvodiBezDovoljnoZaliha(null!));
        }
    }
}
