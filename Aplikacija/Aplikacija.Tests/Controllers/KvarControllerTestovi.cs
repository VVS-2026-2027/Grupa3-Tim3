using Aplikacija.Controllers;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Aplikacija.Tests.Controllers;

[TestClass, TestCategory("Kontroler")]
public class KvarControllerTestovi
{
    [Owner("Adnan"), TestMethod]
    public async Task Index_PraznaKolekcija_VracaPrazanModel()
    {
         
        using var baza = new TestBaza();
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.Index();
          
        Assert.AreEqual(0, Provjere.ModelPrikaza<List<Kvar>>(rezultat).Count);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public async Task Index_PostojeciKvarovi_VracaSveKvaroveIPovezaneUredjaje()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar(20), TestObjekti.Kvar(21));
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.Index();
          
        var model = Provjere.ModelPrikaza<List<Kvar>>(rezultat);
        CollectionAssert.AreEquivalent(new[] { 20, 21 }, model.Select(k => k.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 10, 10 }, model.Select(k => k.Uredjaj.Id).ToArray());
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(null), DataRow(-1), DataRow(0), DataRow(999)]
    public async Task Details_NullIliNepostojeciId_VracaNotFound(int? id)
    {
         
        using var baza = new TestBaza();
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.Details(id);
          
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public async Task Details_PostojeciId_VracaKvarIPovezaniUredjaj()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar());
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.Details(20);
          
        var kvar = Provjere.ModelPrikaza<Kvar>(rezultat);
        Assert.AreEqual(20, kvar.Id);
        Assert.AreEqual("Monitor ne radi", kvar.Opis);
        Assert.AreEqual(10, kvar.Uredjaj.Id);
        Assert.IsInstanceOfType<PC>(kvar.Uredjaj);
    }

    [Owner("Adnan"), TestMethod]
    public void CreateGet_BezUredjaja_VracaPrikazIPrazanIzbor()
    {
         
        using var baza = new TestBaza();
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = kontroler.Create();
          
        Assert.IsNull(Provjere.Prikaz(rezultat).Model);
        var izbor = (SelectList)kontroler.ViewData["UredjajId"]!;
        Assert.AreEqual(0, izbor.Count());
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public void CreateGet_PostojeciUredjaji_PopunjavaIzbor()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Uredjaj("XBox", 11));
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = kontroler.Create();
          
        Provjere.Prikaz(rezultat);
        var izbor = (SelectList)kontroler.ViewData["UredjajId"]!;
        CollectionAssert.AreEquivalent(new[] { "10", "11" }, izbor.Select(x => x.Value).ToArray());
        Assert.AreEqual("Id", izbor.DataTextField);
        Assert.AreEqual("Id", izbor.DataValueField);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(StatusKvara.Prijavljen), DataRow(StatusKvara.UObradi), DataRow(StatusKvara.Otklonjen)]
    public async Task CreatePost_IspravanModel_PostavljaPrijavljenStatusIOznacavaUredjaj(StatusKvara poslaniStatus)
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"));
        var kvar = TestObjekti.Kvar(status: poslaniStatus);
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.Create(kvar);
          
        using var provjera = baza.NoviKontekst();
        var snimljen = await provjera.Kvar.SingleAsync();
        Assert.AreEqual(StatusKvara.Prijavljen, snimljen.Status);
        Assert.AreEqual("Monitor ne radi", snimljen.Opis);
        Assert.AreEqual(10, snimljen.UredjajId);
        Assert.AreEqual(StatusUredjaja.Neispravan, (await provjera.Uredjaj.SingleAsync()).Status);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public async Task CreatePost_NeispravanModel_VracaIstiModelIIzborBezSnimanja()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"));
        var kvar = TestObjekti.Kvar();
        kvar.Opis = "abc";
        var kontroler = new KvarController(baza.Context);
        kontroler.ModelState.AddModelError("Opis", "Opis je prekratak.");
           
        var rezultat = await kontroler.Create(kvar);
          
        Assert.AreSame(kvar, Provjere.ModelPrikaza<Kvar>(rezultat));
        var izbor = (SelectList)kontroler.ViewData["UredjajId"]!;
        Assert.AreEqual(10, izbor.SelectedValue);
        Assert.AreEqual("10", izbor.Single().Value);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.Kvar.CountAsync());
        Assert.AreEqual(StatusUredjaja.Zauzet, (await provjera.Uredjaj.SingleAsync()).Status);
    }

    [Owner("Adnan"), TestMethod]
    public async Task CreatePost_NepostojeciUredjaj_ProsljedjujeGreskuStranogKljuca()
    {
         
        using var baza = new TestBaza();
        var kvar = TestObjekti.Kvar(uredjajId: 999);
        var kontroler = new KvarController(baza.Context);
           
        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => kontroler.Create(kvar));
          
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.Kvar.CountAsync());
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(null), DataRow(-1), DataRow(0), DataRow(999)]
    public async Task EditGet_NullIliNepostojeciId_VracaNotFound(int? id)
    {
         
        using var baza = new TestBaza();
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.Edit(id);
          
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public async Task EditGet_PostojeciId_VracaModelIIzabraniUredjaj()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar());
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.Edit(20);
          
        Assert.AreEqual(20, Provjere.ModelPrikaza<Kvar>(rezultat).Id);
        Assert.AreEqual(10, ((SelectList)kontroler.ViewData["UredjajId"]!).SelectedValue);
    }

    [Owner("Adnan"), TestMethod]
    public async Task EditPost_RazlicitiIdentifikatori_VracaNotFoundBezSnimanja()
    {
         
        using var baza = new TestBaza();
        var kontroler = new KvarController(baza.Context);
        var kvar = TestObjekti.Kvar(20);
           
        var rezultat = await kontroler.Edit(21, kvar);
          
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public async Task EditPost_IspravanModel_SnimaIzmjeneIPreusmjerava()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar());
        var kvar = TestObjekti.Kvar(status: StatusKvara.UObradi);
        kvar.Opis = "Novi opis kvara";
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.Edit(20, kvar);
          
        using var provjera = baza.NoviKontekst();
        var snimljen = await provjera.Kvar.SingleAsync();
        Assert.AreEqual(StatusKvara.UObradi, snimljen.Status);
        Assert.AreEqual("Novi opis kvara", snimljen.Opis);
        Assert.AreEqual(10, snimljen.UredjajId);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public async Task EditPost_NeispravanModel_VracaIstiModelIIzborBezSnimanja()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar());
        var kvar = TestObjekti.Kvar();
        kvar.Opis = "abc";
        var kontroler = new KvarController(baza.Context);
        kontroler.ModelState.AddModelError("Opis", "Opis je prekratak.");
           
        var rezultat = await kontroler.Edit(20, kvar);
          
        Assert.AreSame(kvar, Provjere.ModelPrikaza<Kvar>(rezultat));
        Assert.AreEqual(10, ((SelectList)kontroler.ViewData["UredjajId"]!).SelectedValue);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual("Monitor ne radi", (await provjera.Kvar.SingleAsync()).Opis);
    }

    [Owner("Adnan"), TestMethod, TestCategory("ZamjenskiObjekat")]
    public async Task EditPost_KonkurentnostIZapisObrisan_VracaNotFound()
    {
         
        using var baza = new TestBaza();
        var kvar = TestObjekti.Kvar();
        baza.Context.GreskaPriSnimanju = new DbUpdateConcurrencyException("Zapis je obrisan.");
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.Edit(20, kvar);
          
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.Kvar.CountAsync());
    }

    [Owner("Adnan"), TestMethod, TestCategory("ZamjenskiObjekat")]
    public async Task EditPost_KonkurentnostIZapisPostoji_PonovoBacaIstiIzuzetak()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar());
        var greska = new DbUpdateConcurrencyException("Konkurentna izmjena.");
        baza.Context.GreskaPriSnimanju = greska;
        var kontroler = new KvarController(baza.Context);
        var kvar = TestObjekti.Kvar(status: StatusKvara.UObradi);
           
        var rezultat = await Assert.ThrowsExceptionAsync<DbUpdateConcurrencyException>(() => kontroler.Edit(20, kvar));
          
        Assert.AreSame(greska, rezultat);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(StatusKvara.Prijavljen, (await provjera.Kvar.SingleAsync()).Status);
    }

    [Owner("Adnan"), TestMethod, TestCategory("ZamjenskiObjekat")]
    public async Task EditPost_DrugaGreskaSnimanja_NePretvaraJeUNotFound()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar());
        var greska = new DbUpdateException("Druga greska.");
        baza.Context.GreskaPriSnimanju = greska;
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await Assert.ThrowsExceptionAsync<DbUpdateException>(() => kontroler.Edit(20, TestObjekti.Kvar()));
          
        Assert.AreSame(greska, rezultat);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(null), DataRow(-1), DataRow(0), DataRow(999)]
    public async Task DeleteGet_NullIliNepostojeciId_VracaNotFound(int? id)
    {
         
        using var baza = new TestBaza();
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.Delete(id);
          
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    public async Task DeleteGet_PostojeciId_VracaKvarIPovezaniUredjaj()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar());
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.Delete(20);
          
        var kvar = Provjere.ModelPrikaza<Kvar>(rezultat);
        Assert.AreEqual(20, kvar.Id);
        Assert.AreEqual(10, kvar.Uredjaj.Id);
        Assert.AreEqual(1, await baza.Context.Kvar.CountAsync());
    }

    [Owner("Adnan"), TestMethod]
    public async Task DeleteConfirmed_PostojeciId_BriseKvarICuvaUredjaj()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar());
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.DeleteConfirmed(20);
          
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(0, await provjera.Kvar.CountAsync());
        Assert.AreEqual(1, await provjera.Uredjaj.CountAsync());
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public async Task DeleteConfirmed_NepostojeciId_CuvaPostojeceKvaroveIPreusmjerava()
    {
         
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"), TestObjekti.Kvar());
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.DeleteConfirmed(999);
          
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(20, (await provjera.Kvar.SingleAsync()).Id);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public async Task RijesiKvar_NepostojeciId_VracaNotFoundBezSnimanja()
    {
         
        using var baza = new TestBaza();
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.RijesiKvar(999);
          
        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(0, baza.Context.BrojSnimanja);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(StatusKvara.Prijavljen), DataRow(StatusKvara.UObradi), DataRow(StatusKvara.Otklonjen)]
    public async Task RijesiKvar_PostojeciKvar_PostavljaOtklonjenISlobodan(StatusKvara pocetniStatus)
    {
         
        using var baza = new TestBaza();
        var uredjaj = TestObjekti.Uredjaj("PC");
        uredjaj.Status = StatusUredjaja.Neispravan;
        baza.Dodaj(uredjaj, TestObjekti.Kvar(status: pocetniStatus));
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await kontroler.RijesiKvar(20);
          
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(StatusKvara.Otklonjen, (await provjera.Kvar.SingleAsync()).Status);
        Assert.AreEqual(StatusUredjaja.Slobodan, (await provjera.Uredjaj.SingleAsync()).Status);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod, TestCategory("ZamjenskiObjekat")]
    public async Task RijesiKvar_GreskaSnimanja_ProsljedjujeIzuzetakBezTrajnihPromjena()
    {
         
        using var baza = new TestBaza();
        var uredjaj = TestObjekti.Uredjaj("PC");
        uredjaj.Status = StatusUredjaja.Neispravan;
        baza.Dodaj(uredjaj, TestObjekti.Kvar());
        var greska = new DbUpdateException("Simulirana greska.");
        baza.Context.GreskaPriSnimanju = greska;
        var kontroler = new KvarController(baza.Context);
           
        var rezultat = await Assert.ThrowsExceptionAsync<DbUpdateException>(() => kontroler.RijesiKvar(20));
          
        Assert.AreSame(greska, rezultat);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(StatusKvara.Prijavljen, (await provjera.Kvar.SingleAsync()).Status);
        Assert.AreEqual(StatusUredjaja.Neispravan, (await provjera.Uredjaj.SingleAsync()).Status);
    }
}
