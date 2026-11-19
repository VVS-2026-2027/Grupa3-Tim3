using Aplikacija.Models;
using Aplikacija.Tests.Helpers;

namespace Aplikacija.Tests.Models;

[TestClass]
[TestCategory("Model")]
public class TakmicenjeTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(1000)]
    public void GetId_ReturnsAssignedId(int id)
    {
        var takmicenje = new Takmicenje { Id = id };

        Assert.AreEqual(id, takmicenje.GetId());
    }

    [TestMethod]
    public void Properties_AreStoredCorrectly()
    {
        var datum = new DateTime(2030, 3, 15, 18, 30, 0);

        var takmicenje = new Takmicenje { Naziv = "Turnir", Igra = "CS2", Datum = datum };

        Assert.AreEqual("Turnir", takmicenje.Naziv);
        Assert.AreEqual("CS2", takmicenje.Igra);
        Assert.AreEqual(datum, takmicenje.Datum);
    }

    [TestMethod]
    [DataRow("Turnir", "CS2", true)]
    [DataRow(null, "CS2", false)]
    [DataRow("Turnir", null, false)]
    [DataRow("", "CS2", false)]
    [DataRow("Turnir", "", false)]
    [DataRow(null, null, false)]
    public void Validate_RequiredTextFields_AreEnforced(string? naziv, string? igra, bool expectedValid)
    {
        var takmicenje = new Takmicenje { Naziv = naziv!, Igra = igra!, Datum = new DateTime(2030, 1, 1) };

        Assert.AreEqual(expectedValid, ValidationHelper.IsValid(takmicenje));
    }
}
