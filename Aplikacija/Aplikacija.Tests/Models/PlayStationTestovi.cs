using Aplikacija.Models;

namespace Aplikacija.Tests.Models;

[TestClass, TestCategory("Model")]
public class PlayStationTestovi
{
    [Owner("Adnan"), TestMethod]
    public void Konstruktor_NoviPlayStation_PostavljaPodrazumijevaneVrijednosti()
    {
          
        var ps = new PlayStation();
          
        Assert.IsNull(ps.Naziv);
        Assert.AreEqual(0.0, ps.CijenaPoSatu);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow("PS5"), DataRow(""), DataRow(null)]
    public void GetNaziv_PostavljenaVrijednost_VracaNaziv(string? naziv)
    {
         
        var ps = new PlayStation { Naziv = naziv! };
           
        string? rezultat = ps.GetNaziv();
          
        Assert.AreEqual(naziv, rezultat);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(0.0), DataRow(2.5), DataRow(12.99)]
    public void GetCijenaPoSatu_PostavljenaVrijednost_VracaCijenu(double cijena)
    {
         
        var ps = new PlayStation { CijenaPoSatu = cijena };
           
        double rezultat = ps.GetCijenaPoSatu();
          
        Assert.AreEqual(cijena, rezultat, 0.000001);
    }

    [Owner("Adnan"), TestMethod]
    public void Nasljedjivanje_PlayStationKaoUredjaj_CuvaIdentitetIStatus()
    {
         
        Uredjaj uredjaj = new PlayStation { Id = 8, Status = StatusUredjaja.Neispravan };
           
        string tip = uredjaj.TipUredjaja;
          
        Assert.AreEqual("PlayStation", tip);
        Assert.AreEqual(8, uredjaj.GetId());
        Assert.AreEqual(StatusUredjaja.Neispravan, uredjaj.Status);
    }
}
