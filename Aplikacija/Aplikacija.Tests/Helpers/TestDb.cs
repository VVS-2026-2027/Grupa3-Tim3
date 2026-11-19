using Aplikacija.Models;
using Microsoft.EntityFrameworkCore;

namespace Aplikacija.Tests.Helpers;

/// <summary>Zamjenski (fake) DbContext: baza je InMemory, prava baza nije aktivna.</summary>
public static class TestDb
{
    public static string NewName() => Guid.NewGuid().ToString();

    public static DbContextOptions<ApplicationDbContext> Options(string name)
        => new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;

    public static ApplicationDbContext Create(string name) => new(Options(name));

    public static ThrowingDbContext CreateThrowing(string name) => new(Options(name));

    // ---- seed helperi: jedino mjesto koje treba uskladiti sa vasim modelima ----
    public static void SeedKorisnik(this ApplicationDbContext db, string id)
    {
        db.Korisnik.Add(new Korisnik
        {
            Id = id,
            Email = id + "@test.ba",
            UserName = id + "@test.ba",
            Ime = "Test",
            Prezime = "Korisnik"
        });
        db.SaveChanges();
    }

    public static void SeedDevice(this ApplicationDbContext db, string tip, int id, double cijenaPoSatu)
    {
        Uredjaj uredjaj = tip switch
        {
            "PC" => new PC { Id = id, CijenaPoSatu = cijenaPoSatu, Naziv = "PC " + id, Model = "Model" },
            "PlayStation" => new PlayStation { Id = id, CijenaPoSatu = cijenaPoSatu, Naziv = "PS " + id },
            "XBox" => new XBox { Id = id, CijenaPoSatu = cijenaPoSatu, Naziv = "XBox " + id },
            _ => throw new ArgumentException("Nepoznat tip uredjaja: " + tip)
        };
        db.Uredjaj.Add(uredjaj);
        db.SaveChanges();
    }

    public static void SeedRezervacija(this ApplicationDbContext db, int id, string korisnikId, int uredjajId,
        DateTime od, DateTime @do, StatusRezervacije status)
    {
        db.Rezervacija.Add(new Rezervacija
        {
            Id = id, KorisnikId = korisnikId, UredjajId = uredjajId,
            VrijemeOd = od, VrijemeDo = @do, Status = status
        });
        db.SaveChanges();
    }

    public static void SeedLoyalty(this ApplicationDbContext db, string korisnikId, int bodovi)
    {
        db.LoyaltyProgram.Add(new LoyaltyProgram { KorisnikId = korisnikId, UkupniBodovi = bodovi });
        db.SaveChanges();
    }

    public static void SeedTakmicenje(this ApplicationDbContext db, int id, string naziv, DateTime datum, string igra = "CS2")
    {
        db.Takmicenje.Add(new Takmicenje { Id = id, Naziv = naziv, Igra = igra, Datum = datum });
        db.SaveChanges();
    }
}

/// <summary>Fake context koji baca DbUpdateConcurrencyException pri snimanju (kad je ThrowOnSave = true).</summary>
public class ThrowingDbContext : ApplicationDbContext
{
    public bool ThrowOnSave { get; set; }

    public ThrowingDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnSave)
            throw new DbUpdateConcurrencyException("Fake concurrency failure");
        return base.SaveChangesAsync(cancellationToken);
    }
}
