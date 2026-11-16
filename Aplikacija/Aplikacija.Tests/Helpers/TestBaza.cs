using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Aplikacija.Tests.Helpers;

public sealed class TestBaza : IDisposable
{
    private readonly SqliteConnection veza;
    private readonly DbContextOptions<ApplicationDbContext> options;
    public TestniDbContext Context { get; }

    public TestBaza()
    {
        veza = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        veza.Open();
        options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(veza).Options;
        Context = new TestniDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dodaj(params object[] objekti)
    {
        Context.AddRange(objekti);
        
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

   
    public ApplicationDbContext NoviKontekst() => new(options);

    public void Dispose()
    {
        Context.Dispose();
        veza.Dispose();
    }
}
