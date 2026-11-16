using System.Net;
using System.Text.Json;
using Aplikacija.Controllers;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Castle.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Aplikacija.Tests.Controllers;

/// <summary>
/// ZAMJENSKI OBJEKTI:
///  - ApplicationDbContext  -> InMemory baza (fake), ThrowingDbContext (fake koji baca izuzetak)
///  - IHttpClientFactory    -> Moq mock, vraca HttpClient sa FakeHttpMessageHandler (stub vanjskog PandaScore API-ja)
///  - IConfiguration        -> Moq mock (stub konfiguracije PandaScore:ApiUrl / PandaScore:Token)
/// </summary>
[TestClass]
[TestCategory("Controller")]
public class TakmicenjeControllerTests
{
    private const string Role = "Administrator";
    private const string ApiUrl = "https://fake.pandascore.test/matches";
    private const string Token = "secret-token";
    private const string PorukaPrazno = "Vanjski sistem trenutno nije vratio nijedno takmičenje.";
    private const string PorukaUspjeh = "Takmičenja su učitana iz PandaScore vanjskog sistema.";

    private string _dbName = null!;
    private ApplicationDbContext _seed = null!;
    private Mock<IHttpClientFactory> _factoryMock = null!;
    private FakeHttpMessageHandler _handler = null!;

    [TestInitialize]
    public void Init()
    {
        _dbName = TestDb.NewName();
        _seed = TestDb.Create(_dbName);
    }

    [TestCleanup]
    public void Cleanup() => _seed.Dispose();

    private ApplicationDbContext Fresh() => TestDb.Create(_dbName);

    private static Mock<IConfiguration> ConfigMock()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["PandaScore:ApiUrl"]).Returns(ApiUrl);
        config.Setup(c => c["PandaScore:Token"]).Returns(Token);
        return config;
    }

    /// <summary>Kontroler bez vanjskog sistema (za CRUD testove).</summary>
    private TakmicenjeController Controller(ApplicationDbContext? context = null)
        => new TakmicenjeController(context ?? Fresh(), Mock.Of<IHttpClientFactory>(), Mock.Of<IConfiguration>())
            .WithUser("a1", Role);

    /// <summary>Kontroler cije je vanjski sistem zamijenjen fake-om koji vraca zadani odgovor.</summary>
    private TakmicenjeController ControllerWithApi(HttpStatusCode status, string body)
    {
        _handler = new FakeHttpMessageHandler(status, body);
        _factoryMock = new Mock<IHttpClientFactory>();
        _factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_handler));
        return new TakmicenjeController(Fresh(), _factoryMock.Object, ConfigMock().Object).WithUser("a1", Role);
    }

    private static Takmicenje Novo(int id = 0, string naziv = "Turnir", string igra = "CS2") => new()
    {
        Id = id, Naziv = naziv, Igra = igra, Datum = DateTime.Today.AddDays(3)
    };

    // ===================== INDEX =====================

    [TestMethod]
    public async Task Index_PastCompetition_IsRemovedFromDatabase()
    {
        _seed.SeedTakmicenje(1, "Stari", DateTime.Today.AddDays(-1));
        _seed.SeedTakmicenje(2, "Novi", DateTime.Today.AddDays(1));

        await Controller().Index();

        Assert.AreEqual(1, Fresh().Takmicenje.Count());
    }

    [TestMethod]
    public async Task Index_ReturnsOnlyUpcomingCompetitionsOrderedByDate()
    {
        _seed.SeedTakmicenje(1, "Za 5 dana", DateTime.Today.AddDays(5));
        _seed.SeedTakmicenje(2, "Sutra", DateTime.Today.AddDays(1));
        _seed.SeedTakmicenje(3, "Prosli", DateTime.Today.AddDays(-3));

        var result = await Controller().Index();

        CollectionAssert.AreEqual(new[] { 2, 1 }, result.ModelOf<List<Takmicenje>>().Select(t => t.Id).ToArray());
    }

    [TestMethod]
    public async Task Index_CompetitionToday_IsKept()
    {
        _seed.SeedTakmicenje(1, "Danas", DateTime.Today);

        var result = await Controller().Index();

        Assert.AreEqual(1, result.ModelOf<List<Takmicenje>>().Count);
    }

    [TestMethod]
    public async Task Index_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await Controller().Index();

        Assert.AreEqual(0, result.ModelOf<List<Takmicenje>>().Count);
    }

    // ===================== DETAILS =====================

    [TestMethod]
    public async Task Details_NullId_ReturnsNotFound()
    {
        var result = await Controller().Details(null);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(999)]
    public async Task Details_UnknownId_ReturnsNotFound(int id)
    {
        var result = await Controller().Details(id);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Details_ExistingId_ReturnsViewWithCompetition()
    {
        _seed.SeedTakmicenje(1, "Turnir", DateTime.Today.AddDays(1));

        var result = await Controller().Details(1);

        Assert.AreEqual("Turnir", result.ModelOf<Takmicenje>().Naziv);
    }

    // ===================== CREATE =====================

    [TestMethod]
    public void Create_Get_ReturnsView()
    {
        var result = Controller().Create();

        result.Is<ViewResult>();
    }

    [TestMethod]
    public async Task Create_Post_ValidModel_StoresCompetition()
    {
        await Controller().Create(Novo());

        Assert.AreEqual(1, Fresh().Takmicenje.Count());
    }

    [TestMethod]
    public async Task Create_Post_ValidModel_RedirectsToIndex()
    {
        var result = await Controller().Create(Novo());

        Assert.AreEqual("Index", result.Is<RedirectToActionResult>().ActionName);
    }

    [TestMethod]
    public async Task Create_Post_InvalidModel_ReturnsViewAndStoresNothing()
    {
        var controller = Controller();
        controller.ModelState.AddModelError("Naziv", "Required");

        var result = await controller.Create(Novo());

        result.Is<ViewResult>();
        Assert.AreEqual(0, Fresh().Takmicenje.Count());
    }

    // ===================== EDIT =====================

    [TestMethod]
    public async Task Edit_Get_NullId_ReturnsNotFound()
    {
        var result = await Controller().Edit((int?)null);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Edit_Get_UnknownId_ReturnsNotFound()
    {
        var result = await Controller().Edit(999);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Edit_Get_ExistingId_ReturnsViewWithCompetition()
    {
        _seed.SeedTakmicenje(1, "Turnir", DateTime.Today.AddDays(1));

        var result = await Controller().Edit(1);

        Assert.AreEqual(1, result.ModelOf<Takmicenje>().Id);
    }

    [TestMethod]
    public async Task Edit_Post_IdMismatch_ReturnsNotFound()
    {
        var result = await Controller().Edit(1, Novo(id: 2));

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Edit_Post_InvalidModel_ReturnsView()
    {
        var controller = Controller();
        controller.ModelState.AddModelError("Naziv", "Required");

        var result = await controller.Edit(1, Novo(id: 1));

        result.Is<ViewResult>();
    }

    [TestMethod]
    public async Task Edit_Post_ValidModel_UpdatesCompetition()
    {
        _seed.SeedTakmicenje(1, "Stari naziv", DateTime.Today.AddDays(1));

        await Controller().Edit(1, Novo(id: 1, naziv: "Novi naziv"));

        Assert.AreEqual("Novi naziv", Fresh().Takmicenje.Find(1)!.Naziv);
    }

    [TestMethod]
    public async Task Edit_Post_ValidModel_RedirectsToIndex()
    {
        _seed.SeedTakmicenje(1, "Turnir", DateTime.Today.AddDays(1));

        var result = await Controller().Edit(1, Novo(id: 1));

        Assert.AreEqual("Index", result.Is<RedirectToActionResult>().ActionName);
    }

    [TestMethod]
    public async Task Edit_Post_CompetitionDeletedMeanwhile_ReturnsNotFound()
    {
        // InMemory baza baca DbUpdateConcurrencyException kad se azurira nepostojeci red
        var result = await Controller().Edit(77, Novo(id: 77));

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Edit_Post_ConcurrencyErrorButCompetitionExists_RethrowsException()
    {
        _seed.SeedTakmicenje(1, "Turnir", DateTime.Today.AddDays(1));
        var throwing = TestDb.CreateThrowing(_dbName);
        throwing.ThrowOnSave = true;   // FAKE context baca izuzetak pri snimanju
        var controller = Controller(throwing);

        await Assert.ThrowsExceptionAsync<DbUpdateConcurrencyException>(() => controller.Edit(1, Novo(id: 1)));
    }

    // ===================== DELETE =====================

    [TestMethod]
    public async Task Delete_Get_NullId_ReturnsNotFound()
    {
        var result = await Controller().Delete(null);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Delete_Get_UnknownId_ReturnsNotFound()
    {
        var result = await Controller().Delete(999);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Delete_Get_ExistingId_ReturnsViewWithCompetition()
    {
        _seed.SeedTakmicenje(1, "Turnir", DateTime.Today.AddDays(1));

        var result = await Controller().Delete(1);

        Assert.AreEqual(1, result.ModelOf<Takmicenje>().Id);
    }

    [TestMethod]
    public async Task DeleteConfirmed_ExistingId_RemovesCompetition()
    {
        _seed.SeedTakmicenje(1, "Turnir", DateTime.Today.AddDays(1));

        await Controller().DeleteConfirmed(1);

        Assert.AreEqual(0, Fresh().Takmicenje.Count());
    }

    [TestMethod]
    public async Task DeleteConfirmed_UnknownId_StillRedirectsToIndex()
    {
        var result = await Controller().DeleteConfirmed(999);

        Assert.AreEqual("Index", result.Is<RedirectToActionResult>().ActionName);
    }

    // ===================== UcitajIzVanjskogSistema (zamjenski objekti) =====================

    [TestMethod]
    [DataRow("[]")]
    [DataRow("null")]
    public async Task UcitajIzVanjskogSistema_ApiReturnsNoCompetitions_SetsInfoMessage(string apiJson)
    {
        var controller = ControllerWithApi(HttpStatusCode.OK, apiJson);

        await controller.UcitajIzVanjskogSistema();

        Assert.AreEqual(PorukaPrazno, controller.TempData["Poruka"]);
    }

    [TestMethod]
    public async Task UcitajIzVanjskogSistema_ApiReturnsNothing_RedirectsToIndex()
    {
        var controller = ControllerWithApi(HttpStatusCode.OK, "[]");

        var result = await controller.UcitajIzVanjskogSistema();

        Assert.AreEqual("Index", result.Is<RedirectToActionResult>().ActionName);
    }

    [TestMethod]
    public async Task UcitajIzVanjskogSistema_PastCompetitions_AreRemoved()
    {
        _seed.SeedTakmicenje(1, "Stari", DateTime.Today.AddDays(-2));
        var controller = ControllerWithApi(HttpStatusCode.OK, "[]");

        await controller.UcitajIzVanjskogSistema();

        Assert.AreEqual(0, Fresh().Takmicenje.Count());
    }

    // DATA-DRIVEN (JSON): TestData/takmicenja.json
    [TestMethod]
    [DynamicData(nameof(TestDataLoader.ApiCases), typeof(TestDataLoader), DynamicDataSourceType.Method)]
    public async Task UcitajIzVanjskogSistema_ApiReturnsMatch_StoresMappedCompetition(string apiJson, string ocekivaniNaziv, string ocekivanaIgra)
    {
        var controller = ControllerWithApi(HttpStatusCode.OK, apiJson);

        await controller.UcitajIzVanjskogSistema();

        var spremljeno = Fresh().Takmicenje.Single();
        Assert.AreEqual(ocekivaniNaziv, spremljeno.Naziv);
        Assert.AreEqual(ocekivanaIgra, spremljeno.Igra);
    }

    [TestMethod]
    public async Task UcitajIzVanjskogSistema_ApiReturnsMatch_SetsSuccessMessage()
    {
        var controller = ControllerWithApi(HttpStatusCode.OK, """[{"name":"A","begin_at":"2030-05-01T12:00:00"}]""");

        await controller.UcitajIzVanjskogSistema();

        Assert.AreEqual(PorukaUspjeh, controller.TempData["Poruka"]);
    }

    [TestMethod]
    public async Task UcitajIzVanjskogSistema_ApiReturnsMatch_StoresParsedDate()
    {
        var controller = ControllerWithApi(HttpStatusCode.OK, """[{"name":"A","begin_at":"2030-05-01T12:00:00"}]""");

        await controller.UcitajIzVanjskogSistema();

        Assert.AreEqual(new DateTime(2030, 5, 1, 12, 0, 0), Fresh().Takmicenje.Single().Datum);
    }

    [TestMethod]
    public async Task UcitajIzVanjskogSistema_CompetitionAlreadyExists_IsNotDuplicated()
    {
        _seed.SeedTakmicenje(1, "Match A", new DateTime(2030, 5, 1, 12, 0, 0));
        var controller = ControllerWithApi(HttpStatusCode.OK, """[{"name":"Match A","begin_at":"2030-05-01T12:00:00"}]""");

        await controller.UcitajIzVanjskogSistema();

        Assert.AreEqual(1, Fresh().Takmicenje.Count());
    }

    [TestMethod]
    public async Task UcitajIzVanjskogSistema_ExistingAndNewMatch_OnlyNewIsAdded()
    {
        _seed.SeedTakmicenje(1, "Match A", new DateTime(2030, 5, 1, 12, 0, 0));
        var controller = ControllerWithApi(HttpStatusCode.OK,
            """[{"name":"Match A","begin_at":"2030-05-01T12:00:00"},{"name":"Match B","begin_at":"2030-05-02T12:00:00"}]""");

        await controller.UcitajIzVanjskogSistema();

        Assert.AreEqual(2, Fresh().Takmicenje.Count());
    }

    [TestMethod]
    public async Task UcitajIzVanjskogSistema_UnparsableDate_StoresMinValueDate()
    {
        // Dokumentuje trenutno ponasanje: DateTime.TryParse pri neuspjehu postavlja default(DateTime)
        var controller = ControllerWithApi(HttpStatusCode.OK, """[{"name":"A","begin_at":"nije-datum"}]""");

        await controller.UcitajIzVanjskogSistema();

        Assert.AreEqual(DateTime.MinValue, Fresh().Takmicenje.Single().Datum);
    }

    [TestMethod]
    public async Task UcitajIzVanjskogSistema_SendsBearerTokenFromConfiguration()
    {
        var controller = ControllerWithApi(HttpStatusCode.OK, "[]");

        await controller.UcitajIzVanjskogSistema();

        Assert.AreEqual("Bearer " + Token, _handler.LastRequest!.Headers.GetValues("Authorization").Single());
    }

    [TestMethod]
    public async Task UcitajIzVanjskogSistema_CallsApiUrlFromConfiguration()
    {
        var controller = ControllerWithApi(HttpStatusCode.OK, "[]");

        await controller.UcitajIzVanjskogSistema();

        Assert.AreEqual(ApiUrl, _handler.LastRequest!.RequestUri!.ToString());
    }

    [TestMethod]
    public async Task UcitajIzVanjskogSistema_CreatesHttpClientExactlyOnce()
    {
        var controller = ControllerWithApi(HttpStatusCode.OK, "[]");

        await controller.UcitajIzVanjskogSistema();

        _factoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task UcitajIzVanjskogSistema_ApiReturnsServerError_ThrowsHttpRequestException()
    {
        var controller = ControllerWithApi(HttpStatusCode.InternalServerError, "error");

        await Assert.ThrowsExceptionAsync<HttpRequestException>(() => controller.UcitajIzVanjskogSistema());
    }

    [TestMethod]
    [ExpectedException(typeof(JsonException), AllowDerivedTypes = true)]
    public async Task UcitajIzVanjskogSistema_ApiReturnsMalformedJson_ThrowsJsonException()
    {
        var controller = ControllerWithApi(HttpStatusCode.OK, "<html>not json</html>");

        await controller.UcitajIzVanjskogSistema();
    }
}
