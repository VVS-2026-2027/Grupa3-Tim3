using Aplikacija.Models;
using Moq;

namespace Aplikacija.Tests.Models;

[TestClass, TestCategory("Model")]
public class StrategijaPlacanjaTestovi
{
    private static IStrategijaPlacanja NovaStrategija(string tip, int id = 0) => tip switch
    {
        "Gotovinsko" => new GotovinskoPlacanje { Id = id },
        "Karticno" => new KarticnoPlacanje { Id = id },
        "Loyalty" => new LoyaltyPlacanje { Id = id },
        _ => throw new ArgumentException("Nepoznata strategija placanja.", nameof(tip))
    };

    [TestMethod]
    public void IStrategijaPlacanja_Definicija_JeInterfejsSaJednomMetodom()
    {
        var tip = typeof(IStrategijaPlacanja);

        var metode = tip.GetMethods();

        Assert.IsTrue(tip.IsInterface);
        Assert.AreEqual(1, metode.Length);
        Assert.AreEqual(nameof(IStrategijaPlacanja.ObradiPlacanje), metode[0].Name);
        Assert.AreEqual(typeof(void), metode[0].ReturnType);
        Assert.AreEqual(0, metode[0].GetParameters().Length);
    }

    [TestMethod]
    [DataRow(typeof(GotovinskoPlacanje))]
    [DataRow(typeof(KarticnoPlacanje))]
    [DataRow(typeof(LoyaltyPlacanje))]
    public void Strategija_KonkretnaKlasa_ImplementiraInterfejs(Type tipStrategije)
    {
        bool implementira = typeof(IStrategijaPlacanja).IsAssignableFrom(tipStrategije);

        Assert.IsTrue(implementira);
        Assert.IsFalse(tipStrategije.IsAbstract);
    }

    [TestMethod]
    [DataRow("Gotovinsko")]
    [DataRow("Karticno")]
    [DataRow("Loyalty")]
    public void Konstruktor_NovaStrategija_IdJeNula(string tip)
    {
        var strategija = NovaStrategija(tip);

        int id = ((dynamic)strategija).Id;

        Assert.AreEqual(0, id);
    }

    [TestMethod]
    [DataRow("Gotovinsko", 1)]
    [DataRow("Karticno", 42)]
    [DataRow("Loyalty", int.MaxValue)]
    public void Id_PostavljenaVrijednost_VracaIstuVrijednost(string tip, int id)
    {
        var strategija = NovaStrategija(tip, id);

        int rezultat = ((dynamic)strategija).Id;

        Assert.AreEqual(id, rezultat);
    }

    [TestMethod]
    [DataRow("Gotovinsko")]
    [DataRow("Karticno")]
    [DataRow("Loyalty")]
    public void ObradiPlacanje_PozivPrekoInterfejsa_NeBacaIzuzetakINeMijenjaStanje(string tip)
    {
        var strategija = NovaStrategija(tip, 5);

        strategija.ObradiPlacanje();
        strategija.ObradiPlacanje();

        Assert.AreEqual(5, (int)((dynamic)strategija).Id);
    }

    [TestMethod, TestCategory("ZamjenskiObjekat")]
    public void ObradiPlacanje_ZamjenskaStrategijaKojaBaca_IzuzetakSePropagiraPozivaocu()
    {
        var zamjenska = new Mock<IStrategijaPlacanja>(MockBehavior.Strict);
        zamjenska.Setup(s => s.ObradiPlacanje())
            .Throws(new InvalidOperationException("Kartica odbijena"));
        IStrategijaPlacanja strategija = zamjenska.Object;

        var izuzetak = Assert.ThrowsException<InvalidOperationException>(() => strategija.ObradiPlacanje());

        Assert.AreEqual("Kartica odbijena", izuzetak.Message);
        zamjenska.Verify(s => s.ObradiPlacanje(), Times.Once);
    }
}
