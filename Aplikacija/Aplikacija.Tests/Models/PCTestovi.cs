using Aplikacija.Models;

namespace Aplikacija.Tests.Models;

[TestClass, TestCategory("Model")]
public class PCTestovi
{
    [Owner("Adnan"), TestMethod]
    public void Konstruktor_NoviPC_PostavljaPodrazumijevaneVrijednosti()
    {
          
        var pc = new PC();
          
        Assert.IsNull(pc.Naziv);
        Assert.IsNull(pc.Model);
        Assert.AreEqual(0.0, pc.CijenaPoSatu);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow("Gaming PC"), DataRow(""), DataRow(null)]
    public void GetNaziv_PostavljenaVrijednost_VracaNaziv(string? naziv)
    {
         
        var pc = new PC { Naziv = naziv! };
           
        string? rezultat = pc.GetNaziv();
          
        Assert.AreEqual(naziv, rezultat);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(0.0), DataRow(2.5), DataRow(12.99)]
    public void GetCijenaPoSatu_PostavljenaVrijednost_VracaCijenu(double cijena)
    {
         
        var pc = new PC { CijenaPoSatu = cijena };
           
        double rezultat = pc.GetCijenaPoSatu();
          
        Assert.AreEqual(cijena, rezultat, 0.000001);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow("Model A"), DataRow(""), DataRow(null)]
    public void GetModel_PostavljenaVrijednost_VracaModel(string? model)
    {
         
        var pc = new PC { Model = model! };
           
        string? rezultat = pc.GetModel();
          
        Assert.AreEqual(model, rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public void Nasljedjivanje_PCReferenciranKaoUredjaj_CuvaIdentitetIStatus()
    {
         
        Uredjaj uredjaj = new PC { Id = 7, Status = StatusUredjaja.Rezervisan };
           
        string tip = uredjaj.TipUredjaja;
          
        Assert.AreEqual("PC", tip);
        Assert.AreEqual(7, uredjaj.GetId());
        Assert.AreEqual(StatusUredjaja.Rezervisan, uredjaj.Status);
    }
}
