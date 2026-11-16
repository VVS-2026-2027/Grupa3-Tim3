using Aplikacija.Models;
using Aplikacija.Tests.Helpers;

namespace Aplikacija.Tests.Models;

[TestClass, TestCategory("Model")]
public class TipNarudzbeTestovi
{
    [TestMethod, TestCategory("XML")]
    [DynamicData(nameof(PlacanjeNarudzbaPodaci.TipoviNarudzbeXml), typeof(PlacanjeNarudzbaPodaci), DynamicDataSourceType.Method)]
    public void TipNarudzbe_VrijednostIzXml_ImaOcekivaniNazivIDefinisanost(int vrijednost, string ocekivaniNaziv, bool ocekivanoDefinisan)
    {
        var tip = (TipNarudzbe)vrijednost;

        string naziv = tip.ToString();
        bool definisan = Enum.IsDefined(tip);

        Assert.AreEqual(ocekivaniNaziv, naziv);
        Assert.AreEqual(ocekivanoDefinisan, definisan);
    }

    [TestMethod]
    public void TipNarudzbe_SveVrijednosti_ImaTacnoDvaTipa()
    {
        var vrijednosti = Enum.GetValues<TipNarudzbe>();

        CollectionAssert.AreEqual(new[] { TipNarudzbe.Narudzba, TipNarudzbe.Poruka }, vrijednosti);
    }

    [TestMethod]
    [DataRow("Narudzba", TipNarudzbe.Narudzba)]
    [DataRow("Poruka", TipNarudzbe.Poruka)]
    [DataRow("poruka", TipNarudzbe.Poruka)]
    public void Parse_IspravanNaziv_VracaTip(string naziv, TipNarudzbe ocekivani)
    {
        var tip = Enum.Parse<TipNarudzbe>(naziv, ignoreCase: true);

        Assert.AreEqual(ocekivani, tip);
    }

    [TestMethod]
    [DataRow("Rezervacija")]
    [DataRow("")]
    public void Parse_NepostojeciNaziv_BacaArgumentException(string naziv)
    {
        Assert.ThrowsException<ArgumentException>(() => Enum.Parse<TipNarudzbe>(naziv));
    }
}
