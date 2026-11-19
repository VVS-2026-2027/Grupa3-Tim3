using Aplikacija.Models;

namespace Aplikacija.Tests.Models;

[TestClass, TestCategory("Model")]
public class XBoxTestovi
{
    [Owner("Adnan"), TestMethod]
    public void Konstruktor_NoviXBox_PostavljaPodrazumijevaneVrijednosti()
    {
          
        var xbox = new XBox();
          
        Assert.IsNull(xbox.Naziv);
        Assert.AreEqual(0.0, xbox.CijenaPoSatu);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow("Series X"), DataRow(""), DataRow(null)]
    public void GetNaziv_PostavljenaVrijednost_VracaNaziv(string? naziv)
    {
         
        var xbox = new XBox { Naziv = naziv! };
           
        string? rezultat = xbox.GetNaziv();
          
        Assert.AreEqual(naziv, rezultat);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(0.0), DataRow(2.5), DataRow(12.99)]
    public void GetCijenaPoSatu_PostavljenaVrijednost_VracaCijenu(double cijena)
    {
         
        var xbox = new XBox { CijenaPoSatu = cijena };
           
        double rezultat = xbox.GetCijenaPoSatu();
          
        Assert.AreEqual(cijena, rezultat, 0.000001);
    }

    [Owner("Adnan"), TestMethod]
    public void Nasljedjivanje_XBoxKaoUredjaj_CuvaIdentitetIStatus()
    {
         
        Uredjaj uredjaj = new XBox { Id = 9, Status = StatusUredjaja.Zauzet };
           
        string tip = uredjaj.TipUredjaja;
          
        Assert.AreEqual("XBox", tip);
        Assert.AreEqual(9, uredjaj.GetId());
        Assert.AreEqual(StatusUredjaja.Zauzet, uredjaj.Status);
    }
}
