
using Cvjecara_Latica.Controllers;
using Cvjecara_Latica.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cvjecara_Latica.Tests.NaidaTest;

[TestClass]
public class ReportsControllerTests
{
    [TestMethod]
    public async Task Index_Administrator_VidiSveIzvjestaje()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Users.AddRange(
    NaidaTestHelpers.Osoba("u1"),
    NaidaTestHelpers.Osoba("u2")
);
await db.SaveChangesAsync();

        db.Reports.AddRange(
            NaidaTestHelpers.Izvjestaj(1, "u1"),
            NaidaTestHelpers.Izvjestaj(2, "u2"));
        await db.SaveChangesAsync();

        var c = new ReportsController(db);
        NaidaTestHelpers.Korisnik(c, "u1", true);

        var view = await c.Index() as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreEqual(
            2, ((IEnumerable<Report>)view.Model!).Count());
    }

    [TestMethod]
    public async Task Index_ObicniKorisnik_VidiSamoSvojeIzvjestaje()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Users.AddRange(
    NaidaTestHelpers.Osoba("u1"),
    NaidaTestHelpers.Osoba("u2")
);
await db.SaveChangesAsync();
        db.Reports.AddRange(
            NaidaTestHelpers.Izvjestaj(1, "u1"),
            NaidaTestHelpers.Izvjestaj(2, "u2"));
        await db.SaveChangesAsync();

        var c = new ReportsController(db);
        NaidaTestHelpers.Korisnik(c, "u1", false);

        var view = await c.Index() as ViewResult;
        Assert.IsNotNull(view);

        var izvjestaji =
            ((IEnumerable<Report>)view.Model!).ToList();

        Assert.AreEqual(1, izvjestaji.Count);
        Assert.AreEqual("u1", izvjestaji[0].PersonID);
    }

    [TestMethod]
    public async Task Details_Null_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        Assert.IsInstanceOfType<NotFoundResult>(
            await new ReportsController(db).Details(null));
    }

    [TestMethod]
    public async Task Details_Nepostojeci_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        Assert.IsInstanceOfType<NotFoundResult>(
            await new ReportsController(db).Details(999));
    }

   
[TestMethod]
public async Task Details_Vlasnik_VidiIzvjestaj()
{
    using var db = NaidaTestHelpers.Baza();
   db.Users.Add(NaidaTestHelpers.Osoba("u1"));
await db.SaveChangesAsync();

    db.Reports.Add(NaidaTestHelpers.Izvjestaj());
    await db.SaveChangesAsync();

    var sacuvan = await db.Reports.FindAsync(1);

    Assert.IsNotNull(
        sacuvan,
        "Izvjestaj nije pronadjen u testnoj bazi.");

    
    var saOsobom = await db.Reports
        .Include(r => r.Person)
        .FirstOrDefaultAsync(r => r.ReportID == 1);

    Assert.IsNotNull(
        saOsobom,
        "Izvjestaj nije pronadjen nakon Include(r => r.Person).");

    var c = new ReportsController(db);
    NaidaTestHelpers.Korisnik(c, "u1", false);

    var rezultat = await c.Details(1);

    Assert.IsInstanceOfType<ViewResult>(rezultat);
}


    [TestMethod]
    public async Task Details_NijeVlasnik_VracaForbid()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Users.Add(NaidaTestHelpers.Osoba("u1"));
await db.SaveChangesAsync();
        db.Reports.Add(NaidaTestHelpers.Izvjestaj());
        await db.SaveChangesAsync();

        var c = new ReportsController(db);
        NaidaTestHelpers.Korisnik(c, "u2", false);

        Assert.IsInstanceOfType<ForbidResult>(
            await c.Details(1));
    }

    [TestMethod]
    public async Task Details_Administrator_VidiTudjiIzvjestaj()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Users.Add(NaidaTestHelpers.Osoba("u1"));
await db.SaveChangesAsync();
        db.Reports.Add(NaidaTestHelpers.Izvjestaj());
        await db.SaveChangesAsync();

        var c = new ReportsController(db);
        NaidaTestHelpers.Korisnik(c, "u2", true);

        Assert.IsInstanceOfType<ViewResult>(
            await c.Details(1));
    }

    [TestMethod]
    public void Create_Get_VracaView()
    {
        using var db = NaidaTestHelpers.Baza();

        Assert.IsInstanceOfType<ViewResult>(
            new ReportsController(db).Create());
    }

    [TestMethod]
    public async Task CreatePost_Ispravan_Sprema()
    {
        using var db = NaidaTestHelpers.Baza();

        var rezultat = await new ReportsController(db)
            .Create(NaidaTestHelpers.Izvjestaj());

        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual(1, await db.Reports.CountAsync());
    }

    [TestMethod]
    public async Task CreatePost_Nevalidan_VracaView()
    {
        using var db = NaidaTestHelpers.Baza();
        var c = new ReportsController(db);
        c.ModelState.AddModelError("test", "Greska");

        var rezultat =
            await c.Create(NaidaTestHelpers.Izvjestaj());

        Assert.IsInstanceOfType<ViewResult>(rezultat);
        Assert.AreEqual(0, await db.Reports.CountAsync());
    }

    [TestMethod]
    public async Task EditGet_Null_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        Assert.IsInstanceOfType<NotFoundResult>(
            await new ReportsController(db).Edit((int?)null));
    }

    [TestMethod]
    public async Task EditGet_Nepostojeci_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        Assert.IsInstanceOfType<NotFoundResult>(
            await new ReportsController(db).Edit((int?)999));
    }

    [TestMethod]
    public async Task EditGet_Postojeci_VracaView()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Reports.Add(NaidaTestHelpers.Izvjestaj());
        await db.SaveChangesAsync();

        Assert.IsInstanceOfType<ViewResult>(
            await new ReportsController(db).Edit((int?)1));
    }

    [TestMethod]
    public async Task EditPost_PogresanId_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        Assert.IsInstanceOfType<NotFoundResult>(
            await new ReportsController(db)
                .Edit(2, NaidaTestHelpers.Izvjestaj()));
    }

    [TestMethod]
    public async Task EditPost_Nevalidan_VracaView()
    {
        using var db = NaidaTestHelpers.Baza();
        var c = new ReportsController(db);
        c.ModelState.AddModelError("test", "Greska");

        Assert.IsInstanceOfType<ViewResult>(
            await c.Edit(1, NaidaTestHelpers.Izvjestaj()));
    }

    [TestMethod]
    public async Task EditPost_Ispravan_Redirect()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Reports.Add(NaidaTestHelpers.Izvjestaj());
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.IsInstanceOfType<RedirectToActionResult>(
            await new ReportsController(db)
                .Edit(1, NaidaTestHelpers.Izvjestaj()));
    }

    [TestMethod]
    public async Task DeleteGet_Null_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        Assert.IsInstanceOfType<NotFoundResult>(
            await new ReportsController(db).Delete((int?)null));
    }

    [TestMethod]
    public async Task DeleteGet_Nepostojeci_VracaNotFound()
    {
        using var db = NaidaTestHelpers.Baza();

        Assert.IsInstanceOfType<NotFoundResult>(
            await new ReportsController(db).Delete((int?)999));
    }

    [TestMethod]
    public async Task DeleteGet_Postojeci_VracaView()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Users.Add(NaidaTestHelpers.Osoba("u1"));
await db.SaveChangesAsync();
        db.Reports.Add(NaidaTestHelpers.Izvjestaj());
        await db.SaveChangesAsync();

        Assert.IsInstanceOfType<ViewResult>(
            await new ReportsController(db).Delete((int?)1));
    }

    [TestMethod]
    public async Task DeleteConfirmed_Postojeci_Brise()
    {
        using var db = NaidaTestHelpers.Baza();
        db.Reports.Add(NaidaTestHelpers.Izvjestaj());
        await db.SaveChangesAsync();

        Assert.IsInstanceOfType<RedirectToActionResult>(
            await new ReportsController(db).DeleteConfirmed(1));

        Assert.AreEqual(0, await db.Reports.CountAsync());
    }

    [TestMethod]
    public async Task DeleteConfirmed_Nepostojeci_Redirect()
    {
        using var db = NaidaTestHelpers.Baza();

        Assert.IsInstanceOfType<RedirectToActionResult>(
            await new ReportsController(db).DeleteConfirmed(999));
    }
}
