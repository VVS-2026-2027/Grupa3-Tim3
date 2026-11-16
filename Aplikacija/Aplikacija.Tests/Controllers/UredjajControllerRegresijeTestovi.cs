using Aplikacija.Controllers;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Aplikacija.Tests.Controllers;

// Testovi dokumentuju stvarno ponasanje originalnog kontrolera.
// Prolazak ne znaci da kontroler provodi validaciju: ove akcije je zaobilaze.
[TestClass, TestCategory("PostojecePonasanje")]
public class UredjajControllerRegresijeTestovi
{
    [Owner("Adnan"), TestMethod]
    [DataRow("abcd"), DataRow("Kvar!")]
    public async Task PrijaviKvarPost_OpisNeProlaziAtribute_KontrolerIpakSnimaKvar(string opis)
    {
        // Arrange
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"));
        var kontroler = new UredjajController(baza.Context);
        // Act
        var rezultat = await kontroler.PrijaviKvar(10, opis);
        // Assert
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(1, await provjera.Kvar.CountAsync());

        var kvar = await provjera.Kvar.SingleAsync();
        Assert.AreEqual(opis, kvar.Opis);
        Assert.AreEqual(10, kvar.UredjajId);
        Assert.AreEqual(StatusKvara.Prijavljen, kvar.Status);
        Assert.AreEqual(StatusUredjaja.Neispravan,
            (await provjera.Uredjaj.SingleAsync()).Status);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public async Task CreatePost_NeispravanModelState_KontrolerIpakSnimaStatus99()
    {
        // Arrange
        using var baza = new TestBaza();
        var kontroler = new UredjajController(baza.Context);
        kontroler.ModelState.AddModelError("Status", "Neispravan status.");
        // Act
        var rezultat = await kontroler.Create("PC", (StatusUredjaja)99, "Novi PC", 3.0, "Model A");
        // Assert
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(1, await provjera.Uredjaj.CountAsync());

        var pc = await provjera.PC.SingleAsync();
        Assert.AreEqual((StatusUredjaja)99, pc.Status);
        Assert.AreEqual("Novi PC", pc.Naziv);
        Assert.AreEqual("Model A", pc.Model);
        Assert.AreEqual(3.0, pc.CijenaPoSatu);
        Assert.IsFalse(kontroler.ModelState.IsValid);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }

    [Owner("Adnan"), TestMethod]
    public async Task EditPost_NeispravanModelState_KontrolerIpakMijenjaStatusNa99()
    {
        // Arrange
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.Uredjaj("PC"));
        var kontroler = new UredjajController(baza.Context);
        kontroler.ModelState.AddModelError("Status", "Neispravan status.");
        // Act
        var rezultat = await kontroler.Edit(10, (StatusUredjaja)99, 2.5);
        // Assert
        using var provjera = baza.NoviKontekst();
        var pc = await provjera.PC.SingleAsync();
        Assert.AreEqual(10, pc.Id);
        Assert.AreEqual((StatusUredjaja)99, pc.Status);
        Assert.AreEqual(2.5, pc.CijenaPoSatu);
        Assert.IsFalse(kontroler.ModelState.IsValid);
        Assert.AreEqual(1, baza.Context.BrojSnimanja);
        Provjere.Preusmjerenje(rezultat);
    }
}
