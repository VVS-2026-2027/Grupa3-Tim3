using Aplikacija.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aplikacija.Tests.Services;

[TestClass]
[TestCategory("Servis")]
public class PricingServiceTests
{
    [TestMethod]
    public void NoviServis_PodrazumijevaniId_JeNula()
    {
        var servis = new PricingService();

        Assert.AreEqual(0, servis.Id);
    }

    [TestMethod]
    public void Id_PostavljenaVrijednost_VracaIstuVrijednost()
    {
        var servis = new PricingService { Id = 5 };

        Assert.AreEqual(5, servis.Id);
    }
}
