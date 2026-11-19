using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aplikacija.Tests.Models;

[TestClass]
[TestCategory("Model")]
public class StatusSesijeTests
{
    [TestMethod]
    [DynamicData(nameof(Osoba3Data.StatusiSesijeCsv), typeof(Osoba3Data), DynamicDataSourceType.Method)]
    public void Status_VrijednostIzCsv_ImaOcekivaniNaziv(int vrijednost, string ocekivaniNaziv)
    {
        Assert.AreEqual(ocekivaniNaziv, ((StatusSesije)vrijednost).ToString());
    }

    [TestMethod]
    public void Status_SveVrijednosti_ImaCetiriStatusa()
    {
        Assert.AreEqual(4, Enum.GetValues<StatusSesije>().Length);
    }
}
