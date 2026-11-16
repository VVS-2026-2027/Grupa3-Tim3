using Aplikacija.Models;
using Aplikacija.Tests.Helpers;

namespace Aplikacija.Tests.Models;

[TestClass, TestCategory("Model")]
public class UredjajTestovi
{
    [Owner("Adnan"), TestMethod]
    public void Konstruktor_NoviUredjaj_PostavljaPodrazumijevaneVrijednosti()
    {
          
        var uredjaj = new Uredjaj();
          
        Assert.AreEqual(0, uredjaj.Id);
        Assert.AreEqual(StatusUredjaja.Slobodan, uredjaj.Status);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(0), DataRow(1), DataRow(-1), DataRow(int.MaxValue)]
    public void GetId_PostavljenIdentifikator_VracaPostavljenuVrijednost(int id)
    {
         
        var uredjaj = new Uredjaj { Id = id };
           
        int rezultat = uredjaj.GetId();
          
        Assert.AreEqual(id, rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public void TipUredjaja_OsnovnaKlasa_VracaUredjaj()
    {
         
        var uredjaj = new Uredjaj();
           
        string rezultat = uredjaj.TipUredjaja;
          
        Assert.AreEqual("Uredjaj", rezultat);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(StatusUredjaja.Slobodan), DataRow(StatusUredjaja.Zauzet)]
    [DataRow(StatusUredjaja.Neispravan), DataRow(StatusUredjaja.Rezervisan)]
    public void Status_PromjenaStanja_CuvaNoviStatus(StatusUredjaja status)
    {
         
        var uredjaj = new Uredjaj { Status = StatusUredjaja.Zauzet };
           
        uredjaj.Status = status;
          
        Assert.AreEqual(status, uredjaj.Status);
    }

    [Owner("Adnan"), TestMethod, TestCategory("JSON")]
    [DynamicData(nameof(PodaciIzFajlova.StatusiUredjaja), typeof(PodaciIzFajlova), DynamicDataSourceType.Method)]
    public void Status_JsonPodaci_ValidiraSamoDefinisaneEnumVrijednosti(
        string slucaj, int vrijednost, bool ispravan)
    {
         
        var uredjaj = new Uredjaj { Status = (StatusUredjaja)vrijednost };
           
        var greske = Validacija.ProvjeriSvojstvo(uredjaj, nameof(Uredjaj.Status));
          
        Assert.AreEqual(ispravan, greske.Count == 0, slucaj);
    }
}
