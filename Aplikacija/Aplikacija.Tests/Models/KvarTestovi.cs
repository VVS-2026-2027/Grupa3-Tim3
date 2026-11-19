using Aplikacija.Models;
using Aplikacija.Tests.Helpers;

namespace Aplikacija.Tests.Models;

[TestClass, TestCategory("Model")]
public class KvarTestovi
{
    [Owner("Adnan"), TestMethod]
    public void Konstruktor_NoviKvar_PostavljaPodrazumijevaneVrijednosti()
    {
          
        var kvar = new Kvar();
          
        Assert.AreEqual(0, kvar.Id);
        Assert.IsNull(kvar.Opis);
        Assert.AreEqual(StatusKvara.Prijavljen, kvar.Status);
        Assert.AreEqual(0, kvar.UredjajId);
        Assert.IsNull(kvar.Uredjaj);
        Assert.IsNull(kvar.SesijaId);
        Assert.IsNull(kvar.Sesija);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(0), DataRow(20), DataRow(-1), DataRow(int.MaxValue)]
    public void GetId_PostavljenIdentifikator_VracaVrijednost(int id)
    {
         
        var kvar = new Kvar { Id = id };
           
        int rezultat = kvar.GetId();
          
        Assert.AreEqual(id, rezultat);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow("Monitor ne radi"), DataRow(""), DataRow(null)]
    public void GetOpis_PostavljenaVrijednost_VracaVrijednostBezAutomatskeValidacije(string? opis)
    {
         
        var kvar = new Kvar { Opis = opis! };
           
        string? rezultat = kvar.GetOpis();
          
        Assert.AreEqual(opis, rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public void Uredjaj_PostavljenaVeza_CuvaReferencuIStraniKljuc()
    {
         
        var uredjaj = new Uredjaj { Id = 10 };
        var kvar = new Kvar();
           
        kvar.UredjajId = 10;
        kvar.Uredjaj = uredjaj;
          
        Assert.AreSame(uredjaj, kvar.Uredjaj);
        Assert.AreEqual(10, kvar.UredjajId);
    }

    [Owner("Adnan"), TestMethod]
    public void Sesija_PostavljenaOpcionalnaVeza_CuvaReferencuIStraniKljuc()
    {
         
        var sesija = new Sesija { Id = 30 };
        var kvar = new Kvar();
           
        kvar.SesijaId = 30;
        kvar.Sesija = sesija;
          
        Assert.AreSame(sesija, kvar.Sesija);
        Assert.AreEqual(30, kvar.SesijaId);
    }

    [Owner("Adnan"), TestMethod]
    public void Sesija_UklanjanjeOpcionalneVeze_DozvoljavaNull()
    {
         
        var kvar = new Kvar { SesijaId = 30, Sesija = new Sesija { Id = 30 } };
           
        kvar.SesijaId = null;
        kvar.Sesija = null;
          
        Assert.IsNull(kvar.SesijaId);
        Assert.IsNull(kvar.Sesija);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(StatusKvara.Prijavljen), DataRow(StatusKvara.UObradi), DataRow(StatusKvara.Otklonjen)]
    public void Status_PromjenaStanja_CuvaNoviStatus(StatusKvara status)
    {
         
        var kvar = new Kvar();
           
        kvar.Status = status;
          
        Assert.AreEqual(status, kvar.Status);
    }

    [Owner("Adnan"), TestMethod, TestCategory("CSV")]
    [DynamicData(nameof(PodaciIzFajlova.OpisiKvara), typeof(PodaciIzFajlova), DynamicDataSourceType.Method)]
    public void Opis_CsvPodaci_PostujePostojeceAtribute(
        string slucaj, string? opis, bool ispravan, int brojGresaka)
    {
         
        var kvar = new Kvar { Opis = opis! };
           
        var greske = Validacija.ProvjeriSvojstvo(kvar, nameof(Kvar.Opis));
          
        Assert.AreEqual(ispravan, greske.Count == 0, slucaj);
        Assert.AreEqual(brojGresaka, greske.Count, slucaj);
    }

    [Owner("Adnan"), TestMethod, TestCategory("JSON")]
    [DynamicData(nameof(PodaciIzFajlova.StatusiKvara), typeof(PodaciIzFajlova), DynamicDataSourceType.Method)]
    public void Status_JsonPodaci_PostujeEnumDataType(
        string slucaj, int vrijednost, bool ispravan)
    {
         
        var kvar = new Kvar { Status = (StatusKvara)vrijednost };
           
        var greske = Validacija.ProvjeriSvojstvo(kvar, nameof(Kvar.Status));
          
        Assert.AreEqual(ispravan, greske.Count == 0, slucaj);
    }
}
