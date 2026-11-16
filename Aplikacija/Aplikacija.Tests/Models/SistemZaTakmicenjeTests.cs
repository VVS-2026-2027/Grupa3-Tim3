using Aplikacija.Models;

namespace Aplikacija.Tests.Models;

[TestClass]
[TestCategory("Model")]
public class SistemZaTakmicenjeTests
{
    [TestMethod]
    public void NewInstance_HasDefaultValues()
    {
        var sistem = new SistemZaTakmicenje();

        Assert.AreEqual(0, sistem.Id);
        Assert.IsNull(sistem.NazivSistema);
        Assert.IsNull(sistem.ApiUrl);
        Assert.AreEqual(0, sistem.IntervalOsvjezavanja);
    }

    // DATA-DRIVEN (CSV): TestData/sistemiAD.csv
    [TestMethod]
    [DynamicData(nameof(Aplikacija.Tests.Helpers.TestDataLoader.SistemiRows), typeof(Aplikacija.Tests.Helpers.TestDataLoader), DynamicDataSourceType.Method)]
    public void Properties_AreStoredCorrectly(string naziv, string apiUrl, int interval)
    {
        var sistem = new SistemZaTakmicenje { NazivSistema = naziv, ApiUrl = apiUrl, IntervalOsvjezavanja = interval };

        Assert.AreEqual(naziv, sistem.NazivSistema);
        Assert.AreEqual(apiUrl, sistem.ApiUrl);
        Assert.AreEqual(interval, sistem.IntervalOsvjezavanja);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(99)]
    public void Id_CanBeAssigned(int id)
    {
        var sistem = new SistemZaTakmicenje { Id = id };

        Assert.AreEqual(id, sistem.Id);
    }
}
