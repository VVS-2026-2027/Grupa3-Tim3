using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Aplikacija.Tests.Models;

[TestClass]
public class LoyaltyProgramTests
{
    //  Svojstva i GetUkupniBodovi 

    [Owner("Ajdin.L"), TestMethod]
    public void NoviProgram_ImaPodrazumijevaneVrijednosti()
    {
        var program = new LoyaltyProgram();

        Assert.AreEqual(0, program.Id);
        Assert.AreEqual(0, program.UkupniBodovi);
        Assert.AreEqual(0.0, program.PopustPoBodu);
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(250)]
    [DataRow(int.MaxValue)]
    public void GetUkupniBodovi_VracaPostavljenuVrijednost(int bodovi)
    {
        var program = new LoyaltyProgram { UkupniBodovi = bodovi };

        Assert.AreEqual(bodovi, program.GetUkupniBodovi());
    }

    [Owner("Ajdin.L"), TestMethod]
    public void GetUkupniBodovi_NakonPromjeneBodova_VracaNovuVrijednost()
    {
        var program = new LoyaltyProgram { UkupniBodovi = 10 };

        program.UkupniBodovi += 15;

        Assert.AreEqual(25, program.GetUkupniBodovi());
    }

    [Owner("Ajdin.L"), TestMethod]
    public void PopustPoBodu_ZadrzavaDecimalnuVrijednost()
    {
        var program = new LoyaltyProgram { PopustPoBodu = 0.04 };

        Assert.AreEqual(0.04, program.PopustPoBodu, 1e-9);
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public void GetUkupniBodovi_NegativniBodovi_ModelIhNeOdbija()
    {
        // Model nema validaciju: ogranicenje (ne ispod nule) postoji samo u KorisnikController.
        var program = new LoyaltyProgram { UkupniBodovi = -5 };

        Assert.AreEqual(-5, program.GetUkupniBodovi());
        Assert.IsTrue(Validator.TryValidateObject(program, new ValidationContext(program),
            new List<ValidationResult>(), validateAllProperties: true));
    }

    //  Atributi 

    [Owner("Ajdin.L"), TestMethod]
    public void Id_JeOznacenKaoKljuc()
    {
        Assert.IsNotNull(typeof(LoyaltyProgram).GetProperty(nameof(LoyaltyProgram.Id))!
            .GetCustomAttribute<KeyAttribute>());
    }

    [Owner("Ajdin.L"), TestMethod]
    public void KorisnikId_JeStraniKljucKaKorisniku()
    {
        var atribut = typeof(LoyaltyProgram).GetProperty(nameof(LoyaltyProgram.KorisnikId))!
            .GetCustomAttribute<ForeignKeyAttribute>();

        Assert.IsNotNull(atribut);
        Assert.AreEqual("Korisnik", atribut.Name);
    }

    //  Baza 

    [Owner("Ajdin.L"), TestMethod]
    public void Snimanje_ValidanProgram_DodjeljujeIdIUcitavaKorisnika()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1", "Amar", "Hodzic"));
        var program = new LoyaltyProgram { KorisnikId = "k1", UkupniBodovi = 50, PopustPoBodu = 0.02 };
        
        baza.Dodaj(program);
        
        Assert.IsTrue(program.Id > 0);

        using var provjera = baza.NoviKontekst();
        var ucitan = provjera.LoyaltyProgram.Include(l => l.Korisnik).Single();
        Assert.AreEqual(50, ucitan.GetUkupniBodovi());
        Assert.AreEqual(0.02, ucitan.PopustPoBodu, 1e-9);
        Assert.AreEqual("k1", ucitan.KorisnikId);
        Assert.AreEqual("Amar", ucitan.Korisnik.Ime);
    }

    [Owner("Ajdin.L"), TestMethod]
    public void Snimanje_NepostojeciKorisnik_BacaDbUpdateException()
    {
        using var baza = new TestBaza();
        var program = new LoyaltyProgram { KorisnikId = "nepostoji", UkupniBodovi = 10 };

        TestObjekti.OcekujIzuzetak<DbUpdateException>(() => baza.Dodaj(program));
    }

    [Owner("Ajdin.L"), TestMethod]
    public void Izmjena_PromjenaBodova_TrajnoSeSnimaUBazu()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1"),
            new LoyaltyProgram { KorisnikId = "k1", UkupniBodovi = 10, PopustPoBodu = 0.01 });
        
        var program = baza.Context.LoyaltyProgram.Single();
        program.UkupniBodovi += 40;
        baza.Context.SaveChanges();
       
        using var provjera = baza.NoviKontekst();
        Assert.AreEqual(50, provjera.LoyaltyProgram.Single().GetUkupniBodovi());
    }
}
