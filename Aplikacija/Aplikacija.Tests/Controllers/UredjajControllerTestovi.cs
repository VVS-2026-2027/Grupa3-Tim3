using Aplikacija.Controllers;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aplikacija.Tests.Controllers;

[TestClass, TestCategory("Kontroler")]
public class UredjajControllerTestovi
{
    [Owner("Adnan"), TestMethod]
    public async Task Index_PraznaKolekcija_VracaPrazanModel()
    {
        
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
       
        var rezultat = await kontroler.Index();
       
        Assert.AreEqual(0, Provjere.ModelPrikaza<List<Uredjaj>>(rezultat).Count);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public async Task Index_RazlicitiTipovi_VracaSveUredjaje()
    {
        
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC", 10), TestObjekti.Uredjaj("PlayStation", 11), TestObjekti.Uredjaj("XBox", 12));
        var kontroler = new UredjajController(baza.Context);
        
        var rezultat = await kontroler.Index();
        
        var model = Provjere.ModelPrikaza<List<Uredjaj>>(rezultat);
        CollectionAssert.AreEquivalent(new[] { 10, 11, 12 }, model.Select(u => u.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { "PC", "PlayStation", "XBox" }, model.Select(u => u.TipUredjaja).ToArray());
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(null), DataRow(-1), DataRow(0), DataRow(999)]
    public async Task Details_NullIliNepostojeciId_VracaNotFound(int? id)
    {
       
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
        
        var rezultat = await kontroler.Details(id);
        
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public async Task Details_PostojeciId_VracaTacanModel()
    {
       
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"));
        var kontroler = new UredjajController(baza.Context);
        
        var rezultat = await kontroler.Details(10);
        
        var model = Provjere.ModelPrikaza<PC>(rezultat);
        Assert.AreEqual(10, model.Id);
        Assert.AreEqual("PC sala", model.Naziv);
    }

    [Owner("Adnan"), TestMethod]
    public void Create_Get_VracaPrazanPrikaz()
    {
        
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
        
        var rezultat = kontroler.Create();
        
        Assert.IsNull(Provjere.Prikaz(rezultat).Model);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public async Task Create_PC_SnimaSvaPoljaIPreusmjerava()
    {
      
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
       
        var rezultat = await kontroler.Create("PC", StatusUredjaja.Rezervisan, "Novi PC", 4.25, "Model B");
        
        using var provjera = baza.NoviKontekst();
        var pc = await provjera.PC.SingleAsync();
        Assert.AreEqual("Novi PC", pc.Naziv);
        Assert.AreEqual("Model B", pc.Model);
        Assert.AreEqual(4.25, pc.CijenaPoSatu);
        Assert.AreEqual(StatusUredjaja.Rezervisan, pc.Status);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public async Task Create_PlayStation_SnimaSvaPoljaIPreusmjerava()
    {
        
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
       
        var rezultat = await kontroler.Create("PlayStation", StatusUredjaja.Slobodan, "Novi PS", 3.75, "");
        
        using var provjera = baza.NoviKontekst();
        var ps = await provjera.PlayStation.SingleAsync();
        Assert.AreEqual("Novi PS", ps.Naziv);
        Assert.AreEqual(3.75, ps.CijenaPoSatu);
        Assert.AreEqual(StatusUredjaja.Slobodan, ps.Status);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public async Task Create_XBox_SnimaSvaPoljaIPreusmjerava()
    {
     
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
      
        var rezultat = await kontroler.Create("XBox", StatusUredjaja.Zauzet, "Novi Xbox", 5.0, "");
        
        using var provjera = baza.NoviKontekst();
        var xbox = await provjera.XBox.SingleAsync();
        Assert.AreEqual("Novi Xbox", xbox.Naziv);
        Assert.AreEqual(5.0, xbox.CijenaPoSatu);
        Assert.AreEqual(StatusUredjaja.Zauzet, xbox.Status);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(null), DataRow(""), DataRow("Tablet"), DataRow("pc"), DataRow("Xbox")]
    public async Task Create_NepodrzanaVrsta_VracaPrikazSaGreskomBezSnimanja(string? tip)
    {
        
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
       
        var rezultat = await kontroler.Create(tip!, StatusUredjaja.Slobodan, "Naziv", 2.5, "Model");
        
        Provjere.Prikaz(rezultat);
        Assert.AreEqual("Morate izabrati vrstu uređaja.", kontroler.ModelState[""]!.Errors.Single().ErrorMessage);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
        Assert.AreEqual(0, await baza.Context.Uredjaj.CountAsync());
    }

    [Owner("Adnan"), TestMethod, TestCategory("ZamjenskiObjekat")]
    public async Task Create_GreskaSnimanja_ProsljedjujeIzuzetakBezUpisa()
    {
        
        using var baza = new TestBaza();
        var greska = new DbUpdateException("Simulirana greska snimanja.");
        baza.Context.GreskaPriSnimanju = greska;
        var kontroler = new UredjajController(baza.Context);
        
        var rezultat = await Assert.ThrowsExceptionAsync<DbUpdateException>(() =>
            kontroler.Create("PC", StatusUredjaja.Slobodan, "Novi PC", 3.0, "Model A"));
       
        Assert.AreSame(greska, rezultat);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.Uredjaj.CountAsync());
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(null), DataRow(-1), DataRow(0), DataRow(999)]
    public async Task EditGet_NullIliNepostojeciId_VracaNotFound(int? id)
    {
       
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
        
        var rezultat = await kontroler.Edit(id);
        
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow("PC", "PC sala", 2.5)]
    [DataRow("PlayStation", "PS sala", 3.5)]
    [DataRow("XBox", "Xbox sala", 4.5)]
    public async Task EditGet_PodrzaniTip_VracaModelINazivICijenu(string tip, string naziv, double cijena)
    {
      
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj(tip));
        var kontroler = new UredjajController(baza.Context);
        
        var rezultat = await kontroler.Edit(10);
       
        var model = Provjere.ModelPrikaza<Uredjaj>(rezultat);
        Assert.AreEqual(tip, model.TipUredjaja);
        Assert.AreEqual(10, model.Id);
        Assert.AreEqual(naziv, kontroler.ViewData["Naziv"]);
        Assert.AreEqual(cijena, kontroler.ViewData["CijenaPoSatu"]);
    }

    [Owner("Adnan"), TestMethod]
    public async Task EditGet_OsnovniUredjaj_VracaModelBezSpecificnihPolja()
    {
        
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("Uredjaj"));
        var kontroler = new UredjajController(baza.Context);
        
        var rezultat = await kontroler.Edit(10);
        
        Assert.AreEqual("Uredjaj", Provjere.ModelPrikaza<Uredjaj>(rezultat).TipUredjaja);
        Assert.IsFalse(kontroler.ViewData.ContainsKey("Naziv"));
        Assert.IsFalse(kontroler.ViewData.ContainsKey("CijenaPoSatu"));
    }

    [Owner("Adnan"), TestMethod]
    public async Task EditPost_NepostojeciId_VracaNotFoundBezSnimanja()
    {
        
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
        
        var rezultat = await kontroler.Edit(999, StatusUredjaja.Slobodan, 5.5);
       
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow("PC"), DataRow("PlayStation"), DataRow("XBox")]
    public async Task EditPost_PodrzaniTip_MijenjaStatusICijenu(string tip)
    {
       
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj(tip));
        var kontroler = new UredjajController(baza.Context);
        
        var rezultat = await kontroler.Edit(10, StatusUredjaja.Rezervisan, 6.75);
        
        using var provjera = baza.NoviKontekst();
        var uredjaj = await provjera.Uredjaj.SingleAsync();
        Assert.AreEqual(StatusUredjaja.Rezervisan, uredjaj.Status);
        Assert.AreEqual(6.75, TestObjekti.Cijena(uredjaj));
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public async Task EditPost_OsnovniUredjaj_MijenjaSamoStatus()
    {
        
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("Uredjaj"));
        var kontroler = new UredjajController(baza.Context);
      
        var rezultat = await kontroler.Edit(10, StatusUredjaja.Neispravan, 6.75);
        
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(StatusUredjaja.Neispravan, (await provjera.Uredjaj.SingleAsync()).Status);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(null), DataRow(-1), DataRow(0), DataRow(999)]
    public async Task PrijaviKvarGet_NullIliNepostojeciId_VracaNotFound(int? id)
    {
        
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
       
        var rezultat = await kontroler.PrijaviKvar(id);
      
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public async Task PrijaviKvarGet_PostojeciId_VracaModel()
    {
        
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"));
        var kontroler = new UredjajController(baza.Context);
        
        var rezultat = await kontroler.PrijaviKvar(10);
        
        Assert.AreEqual(10, Provjere.ModelPrikaza<PC>(rezultat).Id);
    }

    [Owner("Adnan"), TestMethod]
    public async Task PrijaviKvarPost_NepostojeciId_VracaNotFoundBezSnimanja()
    {
          
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.PrijaviKvar(999, "Monitor ne radi");
          
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
        Assert.AreEqual(0, await baza.Context.Kvar.CountAsync());
    }

    [Owner("Adnan"), TestMethod]
    public async Task PrijaviKvarPost_PostojeciUredjaj_SnimaPrijavljenKvarINeispravanStatus()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"));
        var kontroler = new UredjajController(baza.Context);
         
        var rezultat = await kontroler.PrijaviKvar(10, "Monitor ne radi");
          
        using var provjera = baza.NoviKontekst();
        var kvar = await provjera.Kvar.SingleAsync();
        Assert.AreEqual("Monitor ne radi", kvar.Opis);
        Assert.AreEqual(10, kvar.UredjajId);
        Assert.AreEqual(StatusKvara.Prijavljen, kvar.Status);
        Assert.IsNull(kvar.SesijaId);
        Assert.AreEqual(StatusUredjaja.Neispravan, (await provjera.Uredjaj.SingleAsync()).Status);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod, TestCategory("ZamjenskiObjekat")]
    public async Task PrijaviKvarPost_GreskaSnimanja_ProsljedjujeIzuzetakBezTrajnihPromjena()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"));
        var greska = new DbUpdateException("Simulirana greska.");
        baza.Context.GreskaPriSnimanju = greska;
        var kontroler = new UredjajController(baza.Context);
          
        var rezultat = await Assert.ThrowsExceptionAsync<DbUpdateException>(() => kontroler.PrijaviKvar(10, "Monitor ne radi"));
          
        Assert.AreSame(greska, rezultat);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.Kvar.CountAsync());
        Assert.AreEqual(StatusUredjaja.Zauzet, (await provjera.Uredjaj.SingleAsync()).Status);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(null), DataRow(-1), DataRow(0), DataRow(999)]
    public async Task DeleteGet_NullIliNepostojeciId_VracaNotFound(int? id)
    {
         
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.Delete(id);
          
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public async Task DeleteGet_PostojeciId_VracaModelPotvrde()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"));
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.Delete(10);
          
        Assert.AreEqual(10, Provjere.ModelPrikaza<PC>(rezultat).Id);
        Assert.AreEqual(1, await baza.Context.Uredjaj.CountAsync());
    }

    [Owner("Adnan"), TestMethod]
    public async Task DeleteConfirmed_PostojeciId_BriseIPreusmjerava()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"));
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.DeleteConfirmed(10);
          
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.Uredjaj.CountAsync());
        Assert.AreEqual(0, await provjera.PC.CountAsync());
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public async Task DeleteConfirmed_NepostojeciId_IpakSnimaIPreusmjerava()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"));
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.DeleteConfirmed(999);
          
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(10, (await provjera.Uredjaj.SingleAsync()).Id);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public async Task DeleteConfirmed_UredjajSaKvarom_ProsljedjujeGreskuStranogKljuca()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar());
        var kontroler = new UredjajController(baza.Context);
           
        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => kontroler.DeleteConfirmed(10));
          
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(1, await provjera.Uredjaj.CountAsync());
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public async Task PC_PraznaKolekcija_VracaPrazanModel()
    {
         
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.PC();
          
        Assert.AreEqual(0, Provjere.ModelPrikaza<List<PC>>(rezultat).Count);
    }

    [Owner("Adnan"), TestMethod]
    public async Task PC_MijesanaKolekcija_VracaSamoRacunare()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Uredjaj("PlayStation", 11), TestObjekti.Uredjaj("XBox", 12));
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.PC();
          
        var pc = Provjere.ModelPrikaza<List<PC>>(rezultat).Single();
        Assert.AreEqual(10, pc.Id);
        Assert.AreEqual("Model A", pc.Model);
    }

    [Owner("Adnan"), TestMethod]
    public async Task PlayStation_PraznaKolekcija_VracaPrazanModel()
    {
         
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.PlayStation();
          
        Assert.AreEqual(0, Provjere.ModelPrikaza<List<PlayStation>>(rezultat).Count);
    }

    [Owner("Adnan"), TestMethod]
    public async Task PlayStation_MijesanaKolekcija_VracaSamoPlayStation()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Uredjaj("PlayStation", 11), TestObjekti.Uredjaj("XBox", 12));
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.PlayStation();
          
        Assert.AreEqual(11, Provjere.ModelPrikaza<List<PlayStation>>(rezultat).Single().Id);
    }

    [Owner("Adnan"), TestMethod]
    public async Task XBox_PraznaKolekcija_VracaPrazanModel()
    {
         
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.XBox();
          
        Assert.AreEqual(0, Provjere.ModelPrikaza<List<XBox>>(rezultat).Count);
    }

    [Owner("Adnan"), TestMethod]
    public async Task XBox_MijesanaKolekcija_VracaSamoXBox()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Uredjaj("PlayStation", 11), TestObjekti.Uredjaj("XBox", 12));
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.XBox();
          
        Assert.AreEqual(12, Provjere.ModelPrikaza<List<XBox>>(rezultat).Single().Id);
    }

    [Owner("Adnan"), TestMethod]
    public async Task VratiUUpotrebu_NepostojeciId_VracaNotFoundBezSnimanja()
    {
         
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.VratiUUpotrebu(999, "PC");
          
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(StatusUredjaja.Slobodan), DataRow(StatusUredjaja.Zauzet)]
    [DataRow(StatusUredjaja.Neispravan), DataRow(StatusUredjaja.Rezervisan)]
    public async Task VratiUUpotrebu_BezKvara_PostavljaSlobodanStatus(StatusUredjaja prethodniStatus)
    {
         
        using var baza = new TestBaza();
        var uredjaj = TestObjekti.Uredjaj("PC");
        uredjaj.Status = prethodniStatus;
        baza.Dodaj(uredjaj);
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.VratiUUpotrebu(10, "PC");
          
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(StatusUredjaja.Slobodan, (await provjera.Uredjaj.SingleAsync()).Status);
        Assert.AreEqual(0, await provjera.Kvar.CountAsync());
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat, "PC");
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(StatusKvara.Prijavljen), DataRow(StatusKvara.UObradi)]
    public async Task VratiUUpotrebu_OtvorenKvar_OtklanjaKvarIOslobadjaUredjaj(StatusKvara status)
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar(status: status));
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.VratiUUpotrebu(10, "PlayStation");
          
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(StatusKvara.Otklonjen, (await provjera.Kvar.SingleAsync()).Status);
        Assert.AreEqual(StatusUredjaja.Slobodan, (await provjera.Uredjaj.SingleAsync()).Status);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat, "PlayStation");
    }

    [Owner("Adnan"), TestMethod]
    public async Task VratiUUpotrebu_OtklonjenITudjiKvarNeOdgovarajuUslovu_RjesavaTacanKvar()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Uredjaj("XBox", 11),
            TestObjekti.Kvar(20), TestObjekti.Kvar(21, 10, StatusKvara.Otklonjen),
            TestObjekti.Kvar(22, 11, StatusKvara.UObradi));
        var kontroler = new UredjajController(baza.Context);
           
        var rezultat = await kontroler.VratiUUpotrebu(10, "Index");
          
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(StatusKvara.Otklonjen, (await provjera.Kvar.FindAsync(20))!.Status);
        Assert.AreEqual(StatusKvara.Otklonjen, (await provjera.Kvar.FindAsync(21))!.Status);
        Assert.AreEqual(StatusKvara.UObradi, (await provjera.Kvar.FindAsync(22))!.Status);
        Assert.AreEqual(StatusUredjaja.Zauzet, (await provjera.Uredjaj.FindAsync(11))!.Status);
        Provjere.Preusmjerenje(rezultat);
    }
}
