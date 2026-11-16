using Aplikacija.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;

namespace Aplikacija.Tests.Helpers;

public static class TestObjekti
{
    public static Uredjaj Uredjaj(string tip, int id = 10) => tip switch
    {
        "PC" => new PC { Id = id, Naziv = "PC sala", Model = "Model A", CijenaPoSatu = 2.5, Status = StatusUredjaja.Zauzet },
        "PlayStation" => new PlayStation { Id = id, Naziv = "PS sala", CijenaPoSatu = 3.5, Status = StatusUredjaja.Zauzet },
        "XBox" => new XBox { Id = id, Naziv = "Xbox sala", CijenaPoSatu = 4.5, Status = StatusUredjaja.Zauzet },
        "Uredjaj" => new Uredjaj { Id = id, Status = StatusUredjaja.Zauzet },
        _ => throw new ArgumentException("Nepoznat tip testnog objekta.", nameof(tip))
    };

    public static double Cijena(Uredjaj uredjaj) => uredjaj switch
    {
        PC pc => pc.CijenaPoSatu,
        PlayStation ps => ps.CijenaPoSatu,
        XBox xbox => xbox.CijenaPoSatu,
        _ => throw new ArgumentException("Osnovni uredjaj nema cijenu.")
    };

    public static Kvar Kvar(int id = 20, int uredjajId = 10,
        StatusKvara status = StatusKvara.Prijavljen) => new()
    {
        Id = id, UredjajId = uredjajId, Opis = "Monitor ne radi", Status = status,
        Uredjaj = null! // Vezu ucitava EF preko postojeceg stranog kljuca.
    };

    //  Identity 

    // Pravi UserManager nad istim kontekstom kao i kontroler (nema potrebe za mockovima).
    public static UserManager<Korisnik> NapraviUserManager(ApplicationDbContext context)
    {
        return new UserManager<Korisnik>(
            new UserStore<Korisnik>(context),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<Korisnik>(),
            new List<IUserValidator<Korisnik>> { new UserValidator<Korisnik>() },
            new List<IPasswordValidator<Korisnik>> { new PasswordValidator<Korisnik>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<Korisnik>>.Instance);
    }

    public static Korisnik NoviKorisnik(string id, string ime = "Amar", string prezime = "Hodzic",
        bool clanarinaAktivna = false)
    {
        var email = $"{id}@test.ba";
        return new Korisnik
        {
            Id = id,
            Ime = ime,
            Prezime = prezime,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            ClanarinaAktivna = clanarinaAktivna
        };
    }

    public static IdentityRole Uloga(string naziv) => new()
    {
        Id = "uloga-" + naziv.ToLowerInvariant(),
        Name = naziv,
        NormalizedName = naziv.ToUpperInvariant()
    };

    public static IdentityUserRole<string> DodjelaUloge(string korisnikId, string naziv) => new()
    {
        UserId = korisnikId,
        RoleId = "uloga-" + naziv.ToLowerInvariant()
    };

    //  HTTP kontekst 

    public static ClaimsPrincipal Anoniman() => new(new ClaimsIdentity());

    public static ClaimsPrincipal Principal(string? korisnikId, params string[] uloge)
    {
        var claims = new List<Claim>();
        if (korisnikId is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, korisnikId));
        claims.AddRange(uloge.Select(u => new Claim(ClaimTypes.Role, u)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    public static ControllerContext KontekstKontrolera(ClaimsPrincipal? principal = null,
        string? traceId = null)
    {
        var httpContext = new DefaultHttpContext { User = principal ?? Anoniman() };
        if (traceId is not null)
            httpContext.TraceIdentifier = traceId;
        return new ControllerContext { HttpContext = httpContext };
    }

    public static TempDataDictionary NoviTempData() =>
        new(new DefaultHttpContext(), new LazniTempDataProvider());

    public static IConfiguration Konfiguracija(string apiUrl, string token) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PandaScore:ApiUrl"] = apiUrl,
                ["PandaScore:Token"] = token
            })
            .Build();

    //  Provjera izuzetaka

    public static async Task<TIzuzetak> OcekujIzuzetakAsync<TIzuzetak>(Func<Task> akcija)
        where TIzuzetak : Exception
    {
        try
        {
            await akcija();
        }
        catch (TIzuzetak izuzetak)
        {
            return izuzetak;
        }
        Assert.Fail($"Ocekivan izuzetak tipa {typeof(TIzuzetak).Name}, ali nije bacen.");
        return null!;
    }

    public static TIzuzetak OcekujIzuzetak<TIzuzetak>(Action akcija)
        where TIzuzetak : Exception
    {
        try
        {
            akcija();
        }
        catch (TIzuzetak izuzetak)
        {
            return izuzetak;
        }
        Assert.Fail($"Ocekivan izuzetak tipa {typeof(TIzuzetak).Name}, ali nije bacen.");
        return null!;
    }
}

public sealed class LazniTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) =>
        new Dictionary<string, object>();

    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}

// HttpMessageHandler koji vraca unaprijed zadan odgovor i pamti zahtjev.
public sealed class LazniHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode status;
    private readonly string tijelo;

    public int BrojZahtjeva { get; private set; }
    public string? Autorizacija { get; private set; }
    public Uri? Adresa { get; private set; }

    public LazniHttpHandler(string tijelo, HttpStatusCode status = HttpStatusCode.OK)
    {
        this.tijelo = tijelo;
        this.status = status;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        BrojZahtjeva++;
        Autorizacija = request.Headers.Authorization?.ToString();
        Adresa = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(tijelo) });
    }
}

public sealed class LazniHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler handler;

    public LazniHttpClientFactory(HttpMessageHandler handler) => this.handler = handler;

    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}