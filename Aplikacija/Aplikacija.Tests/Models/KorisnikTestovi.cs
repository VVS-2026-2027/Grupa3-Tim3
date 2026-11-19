using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aplikacija.Tests.Models;

[TestClass]
public class KorisnikTests
{
    private static readonly string[] ImenaPolja = { nameof(Korisnik.Ime), nameof(Korisnik.Prezime) };

    private static Korisnik Ispravan() => new() { Ime = "Amar", Prezime = "Hodžić" };

    private static Korisnik Sa(string polje, string? vrijednost)
    {
        var korisnik = Ispravan();
        typeof(Korisnik).GetProperty(polje)!.SetValue(korisnik, vrijednost);
        return korisnik;
    }

    private static List<ValidationResult> Validiraj(Korisnik korisnik)
    {
        var rezultati = new List<ValidationResult>();
        Validator.TryValidateObject(korisnik, new ValidationContext(korisnik), rezultati,
            validateAllProperties: true);
        return rezultati;
    }

    private static List<ValidationResult> GreskeZa(Korisnik korisnik, string polje) =>
        Validiraj(korisnik).Where(r => r.MemberNames.Contains(polje)).ToList();

    //  Osnovno 

    [Owner("Ajdin.L"), TestMethod]
    public void Korisnik_JeIdentityUser()
    {
        Assert.IsInstanceOfType<IdentityUser>(new Korisnik());
    }

    [Owner("Ajdin.L"), TestMethod]
    public void NoviKorisnik_ClanarinaJeNeaktivnaPoDefaultu()
    {
        Assert.IsFalse(new Korisnik().ClanarinaAktivna);
    }

    [Owner("Ajdin.L"), TestMethod]
    public void ClanarinaAktivna_MozeSeMijenjati()
    {
        var korisnik = new Korisnik { ClanarinaAktivna = true };
        Assert.IsTrue(korisnik.ClanarinaAktivna);

        korisnik.ClanarinaAktivna = false;
        Assert.IsFalse(korisnik.ClanarinaAktivna);
    }

    [Owner("Ajdin.L"), TestMethod]
    public void IspravanKorisnik_ProlaziValidaciju()
    {
        Assert.AreEqual(0, Validiraj(Ispravan()).Count);
    }

    //  Required 

    [Owner("Ajdin.L"), TestMethod]
    public void ImeIPrezime_Null_NijeValidno()
    {
        foreach (var polje in ImenaPolja)
            Assert.AreEqual(1, GreskeZa(Sa(polje, null), polje).Count, $"Polje: {polje}");
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(""), DataRow(" "), DataRow("   ")]
    public void ImeIPrezime_PrazanTekst_NijeValidno(string vrijednost)
    {
        foreach (var polje in ImenaPolja)
            Assert.IsTrue(GreskeZa(Sa(polje, vrijednost), polje).Count > 0, $"Polje: {polje}");
    }

    //  Duzina (2-30) 

    [Owner("Ajdin.L"), TestMethod]
    [DataRow(1, false)]
    [DataRow(2, true)]
    [DataRow(3, true)]
    [DataRow(29, true)]
    [DataRow(30, true)]
    [DataRow(31, false)]
    public void ImeIPrezime_GranicneDuzine(int duzina, bool ocekivanoValidno)
    {
        var vrijednost = new string('a', duzina);

        foreach (var polje in ImenaPolja)
        {
            var greske = GreskeZa(Sa(polje, vrijednost), polje);
            Assert.AreEqual(ocekivanoValidno, greske.Count == 0, $"Polje: {polje}, duzina: {duzina}");
        }
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow("Ime", "Name must be between 2 and 30 characters long!")]
    [DataRow("Prezime", "Surname must be between 2 and 30 characters long!")]
    public void PrekratkoIme_VracaPorukuODuzini(string polje, string poruka)
    {
        var greske = GreskeZa(Sa(polje, "A"), polje);

        Assert.AreEqual(1, greske.Count);
        Assert.AreEqual(poruka, greske[0].ErrorMessage);
    }

    //  Regex (slova i razmaci) 

    [Owner("Ajdin.L"), TestMethod]
    [DataRow("Ana Marija")]
    [DataRow("Šđčćž")]
    [DataRow("ŠĐČĆŽ")]
    [DataRow("Đorđe Ćosić")]
    [DataRow("Ab")]
    public void ImeIPrezime_SlovaIRazmaci_SuValidni(string vrijednost)
    {
        foreach (var polje in ImenaPolja)
            Assert.AreEqual(0, GreskeZa(Sa(polje, vrijednost), polje).Count, $"Polje: {polje}");
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow("Amar1")]
    [DataRow("12")]
    [DataRow("Amar!")]
    [DataRow("Ana-Marija")]
    [DataRow("Amar_")]
    [DataRow("amar@")]
    [DataRow("Ana.")]
    [DataRow("O'Brien")]
    public void ImeIPrezime_ZnakoviKojiNisuSlova_NisuValidni(string vrijednost)
    {
        foreach (var polje in ImenaPolja)
            Assert.IsTrue(GreskeZa(Sa(polje, vrijednost), polje).Count > 0,
                $"Polje: {polje}, vrijednost: {vrijednost}");
    }

    [Owner("Ajdin.L"), TestMethod]
    [DataRow("Ime", "Name can only contain letters and spaces!")]
    [DataRow("Prezime", "Surname can only contain letters and spaces!")]
    public void NeispravniZnakovi_VracajuPorukuORegexu(string polje, string poruka)
    {
        var greske = GreskeZa(Sa(polje, "Amar1"), polje);

        Assert.AreEqual(1, greske.Count);
        Assert.AreEqual(poruka, greske[0].ErrorMessage);
    }

    [Owner("Ajdin.L"), TestMethod]
    public void NeispravnoIme_NeUtjeceNaPrezime()
    {
        var korisnik = Sa("Ime", "Amar1");

        Assert.IsTrue(GreskeZa(korisnik, "Ime").Count > 0);
        Assert.AreEqual(0, GreskeZa(korisnik, "Prezime").Count);
    }

    //  Atributi 

    [Owner("Ajdin.L"), TestMethod]
    [DataRow("Ime", "Name: ")]
    [DataRow("Prezime", "Surname: ")]
    [DataRow("ClanarinaAktivna", "Membership Active: ")]
    public void Svojstvo_ImaOcekivaniDisplayName(string svojstvo, string ocekivano)
    {
        var atribut = typeof(Korisnik).GetProperty(svojstvo)!.GetCustomAttribute<DisplayNameAttribute>();

        Assert.IsNotNull(atribut);
        Assert.AreEqual(ocekivano, atribut.DisplayName);
    }

    //  Baza 

    [Owner("Ajdin.L"), TestMethod]
    public void Snimanje_IspravanKorisnik_UcitavaSeSaIstimPodacima()
    {
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1", "Amar", "Hodžić", clanarinaAktivna: true));
        
        using var provjera = baza.NoviKontekst();
        var korisnik = provjera.Korisnik.Single();
        
        Assert.AreEqual("k1", korisnik.Id);
        Assert.AreEqual("Amar", korisnik.Ime);
        Assert.AreEqual("Hodžić", korisnik.Prezime);
        Assert.IsTrue(korisnik.ClanarinaAktivna);
    }

    [Owner("Ajdin.L"), TestMethod]
    public void Snimanje_BezImena_BacaDbUpdateExceptionJerJeKolonaObavezna()
    {
        using var baza = new TestBaza();
        var korisnik = TestObjekti.NoviKorisnik("k1");
        korisnik.Ime = null!;

        TestObjekti.OcekujIzuzetak<DbUpdateException>(() => baza.Dodaj(korisnik));
    }

    [Owner("Ajdin.L"), TestMethod, TestCategory("PostojecePonasanje")]
    public void Snimanje_NeispravnoIme_EfNeProvodiRegexValidaciju()
    {
        // Atributi se provjeravaju tek u MVC-u (ModelState), a ne pri snimanju u bazu.
        using var baza = new TestBaza();
        baza.Dodaj(TestObjekti.NoviKorisnik("k1", ime: "12345!"));

        using var provjera = baza.NoviKontekst();
        Assert.AreEqual("12345!", provjera.Korisnik.Single().Ime);
    }
}
