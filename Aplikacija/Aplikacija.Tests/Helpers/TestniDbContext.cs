using Microsoft.EntityFrameworkCore;

namespace Aplikacija.Tests.Helpers;

public sealed class TestniDbContext : ApplicationDbContext
{
    public int BrojSnimanja { get; private set; }
    public Exception? GreskaPriSnimanju { get; set; }

    public TestniDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        BrojSnimanja++;
        if (GreskaPriSnimanju is not null)
            return Task.FromException<int>(GreskaPriSnimanju);

        return base.SaveChangesAsync(cancellationToken);
    }
}
