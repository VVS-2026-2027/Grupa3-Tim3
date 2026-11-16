using Aplikacija.Controllers;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Aplikacija.Tests.Controllers;

/// <summary>
/// Kontroler koristi ApplicationDbContext -> kao zamjenski objekat koristi se InMemory baza (fake),
/// prava baza nije aktivna. Seed ide kroz _seed context, kontroler dobija svjez context nad istom bazom.
/// </summary>
[TestClass]
[TestCategory("Controller")]
public class RezervacijaControllerTests
{
    private const string RoleAdmin = "Administrator";
    private const string RoleUser = "User";
    private const string RoleEmployee = "Employee";
    private static readonly DateTime Dan = new(2030, 1, 1);

    private string _dbName = null!;
    private ApplicationDbContext _seed = null!;

    [TestInitialize]
    public void Init()
    {
        _dbName = TestDb.NewName();
        _seed = TestDb.Create(_dbName);
    }

    [TestCleanup]
    public void Cleanup() => _seed.Dispose();

    private ApplicationDbContext Fresh() => TestDb.Create(_dbName);

    private RezervacijaController Controller(string role = RoleAdmin, string userId = "u1", string? email = null)
        => new RezervacijaController(Fresh()).WithUser(userId, role, email);

    private void SeedBasics()
    {
        _seed.SeedKorisnik("u1");
        _seed.SeedKorisnik("u2");
        _seed.SeedDevice("PC", 1, 5);
    }

    private void SeedAllDevices()
    {
        _seed.SeedKorisnik("u1");
        _seed.SeedDevice("PC", 1, 5);
        _seed.SeedDevice("PlayStation", 2, 8);
        _seed.SeedDevice("XBox", 3, 6);
    }

    private static Rezervacija NovaRezervacija(int id = 0, string korisnik = "u1", int uredjaj = 1) => new()
    {
        Id = id, KorisnikId = korisnik, UredjajId = uredjaj, Status = StatusRezervacije.Aktivna,
        VrijemeOd = Dan.AddHours(10), VrijemeDo = Dan.AddHours(11)
    };

    // ===================== INDEX =====================

    [TestMethod]
    public async Task Index_ActiveReservationInPast_MarksItAsExpired()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, DateTime.Now.AddDays(-2), DateTime.Now.AddDays(-1), StatusRezervacije.Aktivna);

        await Controller().Index();

        Assert.AreEqual(StatusRezervacije.Istekla, Fresh().Rezervacija.Find(1)!.Status);
    }

    [TestMethod]
    public async Task Index_ActiveReservationInFuture_StaysActive()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), StatusRezervacije.Aktivna);

        await Controller().Index();

        Assert.AreEqual(StatusRezervacije.Aktivna, Fresh().Rezervacija.Find(1)!.Status);
    }

    [TestMethod]
    public async Task Index_AsAdministrator_ReturnsAllReservations()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), StatusRezervacije.Aktivna);
        _seed.SeedRezervacija(2, "u2", 1, DateTime.Now.AddDays(3), DateTime.Now.AddDays(4), StatusRezervacije.Aktivna);

        var result = await Controller(RoleAdmin).Index();

        Assert.AreEqual(2, result.ModelOf<List<Rezervacija>>().Count);
    }

    [TestMethod]
    public async Task Index_AsUser_ReturnsOnlyOwnReservations()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), StatusRezervacije.Aktivna);
        _seed.SeedRezervacija(2, "u2", 1, DateTime.Now.AddDays(3), DateTime.Now.AddDays(4), StatusRezervacije.Aktivna);

        var result = await Controller(RoleUser, "u1").Index();

        Assert.AreEqual("u1", result.ModelOf<List<Rezervacija>>().Single().KorisnikId);
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
    [DataRow(-5)]
    [DataRow(999)]
    public async Task Details_UnknownId_ReturnsNotFound(int id)
    {
        SeedBasics();

        var result = await Controller().Details(id);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Details_ExistingId_ReturnsViewWithReservation()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, Dan.AddHours(10), Dan.AddHours(11), StatusRezervacije.Aktivna);

        var result = await Controller().Details(1);

        Assert.AreEqual(1, result.ModelOf<Rezervacija>().Id);
    }

    // ===================== CREATE (admin) =====================

    [TestMethod]
    public void Create_Get_ReturnsViewWithSelectLists()
    {
        var result = Controller().Create();

        Assert.IsNotNull(result.Is<ViewResult>().ViewData["KorisnikId"]);
        Assert.IsNotNull(result.Is<ViewResult>().ViewData["UredjajId"]);
    }

    // DATA-DRIVEN (CSV): TestData/placanja.csv
    [TestMethod]
    [DynamicData(nameof(TestDataLoader.PaymentRows), typeof(TestDataLoader), DynamicDataSourceType.Method)]
    public async Task Create_Post_ValidReservation_CreatesPaymentWithExpectedAmount(string tip, double cijena, double sati, double ocekivaniIznos)
    {
        _seed.SeedKorisnik("u1");
        _seed.SeedDevice(tip, 1, cijena);
        var rezervacija = NovaRezervacija();
        rezervacija.VrijemeDo = rezervacija.VrijemeOd.AddHours(sati);

        await Controller().Create(rezervacija, "Card");

        Assert.AreEqual(ocekivaniIznos, Fresh().Placanje.Single().Iznos, 0.001);
    }

    [TestMethod]
    public async Task Create_Post_ValidReservation_RedirectsToIndex()
    {
        SeedBasics();

        var result = await Controller().Create(NovaRezervacija(), "Card");

        Assert.AreEqual("Index", result.Is<RedirectToActionResult>().ActionName);
    }

    [TestMethod]
    public async Task Create_Post_WithoutPaymentMethod_UsesCashAsDefault()
    {
        SeedBasics();

        await Controller().Create(NovaRezervacija());

        Assert.AreEqual("Cash", Fresh().Placanje.Single().MetodaPlacanja);
    }

    [TestMethod]
    public async Task Create_Post_ValidReservation_StoresReservationInDatabase()
    {
        SeedBasics();

        await Controller().Create(NovaRezervacija());

        Assert.AreEqual(1, Fresh().Rezervacija.Count());
    }

    [TestMethod]
    public async Task Create_Post_InvalidModel_ReturnsSameViewAndStoresNothing()
    {
        SeedBasics();
        var controller = Controller();
        controller.ModelState.AddModelError("VrijemeOd", "Invalid");
        var rezervacija = NovaRezervacija();

        var result = await controller.Create(rezervacija);

        Assert.AreSame(rezervacija, result.ModelOf<Rezervacija>());
        Assert.AreEqual(0, Fresh().Rezervacija.Count());
    }

    // ===================== EDIT (admin) =====================

    [TestMethod]
    public async Task Edit_Get_NullId_ReturnsNotFound()
    {
        var result = await Controller().Edit((int?)null);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Edit_Get_UnknownId_ReturnsNotFound()
    {
        SeedBasics();

        var result = await Controller().Edit(999);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Edit_Get_ExistingId_ReturnsViewWithStatusList()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, Dan.AddHours(10), Dan.AddHours(11), StatusRezervacije.Aktivna);

        var result = await Controller().Edit(1);

        Assert.IsNotNull(result.Is<ViewResult>().ViewData["Status"]);
    }

    [TestMethod]
    public async Task Edit_Post_IdMismatch_ReturnsNotFound()
    {
        var result = await Controller().Edit(1, NovaRezervacija(id: 2));

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Edit_Post_InvalidModel_ReturnsView()
    {
        var controller = Controller();
        controller.ModelState.AddModelError("Status", "Invalid");

        var result = await controller.Edit(1, NovaRezervacija(id: 1));

        result.Is<ViewResult>();
    }

    [TestMethod]
    public async Task Edit_Post_NonExistingReservation_ReturnsNotFound()
    {
        var result = await Controller().Edit(5, NovaRezervacija(id: 5));

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Edit_Post_ValidData_UpdatesReservation()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, Dan.AddHours(10), Dan.AddHours(11), StatusRezervacije.Aktivna);
        var izmijenjena = NovaRezervacija(id: 1, korisnik: "u2");
        izmijenjena.Status = StatusRezervacije.Zavrsena;

        await Controller().Edit(1, izmijenjena);

        var spremljena = Fresh().Rezervacija.Find(1)!;
        Assert.AreEqual(StatusRezervacije.Zavrsena, spremljena.Status);
        Assert.AreEqual("u2", spremljena.KorisnikId);
    }

    [TestMethod]
    public async Task Edit_Post_ValidData_RedirectsToIndex()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, Dan.AddHours(10), Dan.AddHours(11), StatusRezervacije.Aktivna);

        var result = await Controller().Edit(1, NovaRezervacija(id: 1));

        Assert.AreEqual("Index", result.Is<RedirectToActionResult>().ActionName);
    }

    // ===================== DELETE (admin) =====================

    [TestMethod]
    public async Task Delete_Get_NullId_ReturnsNotFound()
    {
        var result = await Controller().Delete(null);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Delete_Get_UnknownId_ReturnsNotFound()
    {
        SeedBasics();

        var result = await Controller().Delete(999);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task Delete_Get_ExistingId_ReturnsViewWithReservation()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, Dan.AddHours(10), Dan.AddHours(11), StatusRezervacije.Aktivna);

        var result = await Controller().Delete(1);

        Assert.AreEqual(1, result.ModelOf<Rezervacija>().Id);
    }

    [TestMethod]
    public async Task DeleteConfirmed_ExistingId_RemovesReservation()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, Dan.AddHours(10), Dan.AddHours(11), StatusRezervacije.Aktivna);

        await Controller().DeleteConfirmed(1);

        Assert.AreEqual(0, Fresh().Rezervacija.Count());
    }

    [TestMethod]
    public async Task DeleteConfirmed_UnknownId_StillRedirectsToIndex()
    {
        var result = await Controller().DeleteConfirmed(999);

        Assert.AreEqual("Index", result.Is<RedirectToActionResult>().ActionName);
    }

    // ===================== USER CREATE (GET) =====================

    private static List<int> ZauzetiSati(IActionResult result)
        => (List<int>)result.Is<ViewResult>().ViewData["ZauzetiSati"]!;

    [TestMethod]
    public async Task UserCreate_Get_NoDevicesOfType_AllHoursAreBusy()
    {
        _seed.SeedKorisnik("u1");

        var result = await Controller(RoleUser).UserCreate(Dan, "PC");

        Assert.AreEqual(13, ZauzetiSati(result).Count);
    }

    [TestMethod]
    public async Task UserCreate_Get_DeviceFree_NoBusyHours()
    {
        SeedBasics();

        var result = await Controller(RoleUser).UserCreate(Dan, "PC");

        Assert.AreEqual(0, ZauzetiSati(result).Count);
    }

    [TestMethod]
    public async Task UserCreate_Get_AllDevicesBookedAt10_HourIsBusy()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, Dan.AddHours(10), Dan.AddHours(11), StatusRezervacije.Aktivna);

        var result = await Controller(RoleUser).UserCreate(Dan, "PC");

        CollectionAssert.Contains(ZauzetiSati(result), 10);
    }

    [TestMethod]
    public async Task UserCreate_Get_CanceledReservation_DoesNotBlockHour()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, Dan.AddHours(10), Dan.AddHours(11), StatusRezervacije.Otkazana);

        var result = await Controller(RoleUser).UserCreate(Dan, "PC");

        CollectionAssert.DoesNotContain(ZauzetiSati(result), 10);
    }

    [TestMethod]
    [DataRow("PC", 1, 5.0)]
    [DataRow("PlayStation", 2, 8.0)]
    [DataRow("XBox", 3, 6.0)]
    public async Task UserCreate_Get_ReturnsPricePerHourForDeviceType(string tip, int id, double cijena)
    {
        _seed.SeedKorisnik("u1");
        _seed.SeedDevice(tip, id, cijena);

        var result = await Controller(RoleUser).UserCreate(Dan, tip);

        Assert.AreEqual(cijena, (double)result.Is<ViewResult>().ViewData["CijenaPoSatu"]!);
    }

    [TestMethod]
    public async Task UserCreate_Get_UnknownDeviceType_PriceIsZero()
    {
        SeedAllDevices();

        var result = await Controller(RoleUser).UserCreate(Dan, "Laptop");

        Assert.AreEqual(0.0, (double)result.Is<ViewResult>().ViewData["CijenaPoSatu"]!);
    }

    [TestMethod]
    public async Task UserCreate_Get_NoDate_UsesToday()
    {
        SeedBasics();

        var result = await Controller(RoleUser).UserCreate(null);

        Assert.AreEqual(DateTime.Today, (DateTime)result.Is<ViewResult>().ViewData["OdabraniDatum"]!);
    }

    [TestMethod]
    public async Task UserCreate_Get_UserHasLoyalty_ReturnsPoints()
    {
        SeedBasics();
        _seed.SeedLoyalty("u1", 120);

        var result = await Controller(RoleUser, "u1").UserCreate(Dan);

        Assert.AreEqual(120, (int)result.Is<ViewResult>().ViewData["LoyaltyPoints"]!);
    }

    [TestMethod]
    public async Task UserCreate_Get_UserWithoutLoyalty_PointsAreZero()
    {
        SeedBasics();

        var result = await Controller(RoleUser, "u1").UserCreate(Dan);

        Assert.AreEqual(0, (int)result.Is<ViewResult>().ViewData["LoyaltyPoints"]!);
    }

    // ===================== USER CREATE (POST) =====================

    [TestMethod]
    [DataRow("PC", "10", 5.0)]
    [DataRow("PC", "10,11,12", 15.0)]
    [DataRow("PlayStation", "9,10", 16.0)]
    [DataRow("XBox", "20,21", 12.0)]
    [DataRow("Laptop", "10", 0.0)]
    public async Task UserCreate_Post_CalculatesTotalPrice(string tip, string sati, double ocekivano)
    {
        SeedAllDevices();

        var result = await Controller(RoleUser).UserCreate(Dan, sati, tip, "Cash", 0);

        Assert.AreEqual(ocekivano, (double)result.Is<ViewResult>().ViewData["TotalPrice"]!);
    }

    [TestMethod]
    public async Task UserCreate_Post_ReturnsConfirmPaymentView()
    {
        SeedAllDevices();

        var result = await Controller(RoleUser).UserCreate(Dan, "10", "PC", "Cash", 0);

        Assert.AreEqual("ConfirmPayment", result.Is<ViewResult>().ViewName);
    }

    // ===================== START SESSION (employee) =====================

    [TestMethod]
    public async Task StartSession_UnknownReservation_ReturnsNotFound()
    {
        var result = await Controller(RoleEmployee).StartSession(999);

        result.Is<NotFoundResult>();
    }

    [TestMethod]
    public async Task StartSession_ExistingReservation_CreatesActiveSession()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, Dan.AddHours(10), Dan.AddHours(11), StatusRezervacije.Zavrsena);

        await Controller(RoleEmployee).StartSession(1);

        Assert.AreEqual(StatusSesije.Aktivna, Fresh().Sesija.Single().Status);
    }

    [TestMethod]
    public async Task StartSession_ExistingReservation_SetsReservationToActive()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, Dan.AddHours(10), Dan.AddHours(11), StatusRezervacije.Zavrsena);

        await Controller(RoleEmployee).StartSession(1);

        Assert.AreEqual(StatusRezervacije.Aktivna, Fresh().Rezervacija.Find(1)!.Status);
    }

    [TestMethod]
    public async Task StartSession_ExistingReservation_RedirectsToIndex()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, Dan.AddHours(10), Dan.AddHours(11), StatusRezervacije.Aktivna);

        var result = await Controller(RoleEmployee).StartSession(1);

        Assert.AreEqual("Index", result.Is<RedirectToActionResult>().ActionName);
    }

    // ===================== CONFIRM PAYMENT =====================

    private Task<IActionResult> Pay(RezervacijaController c, string sati = "10", string tip = "PC",
        double ukupno = 10, int bodovi = 0, DateTime? datum = null)
        => c.ConfirmPayment(datum ?? DateTime.Today.AddDays(1), sati, tip, "Cash", ukupno, bodovi);

    [TestMethod]
    public async Task ConfirmPayment_UnknownUser_RedirectsToUserCreate()
    {
        var result = await Pay(Controller(RoleUser, "nepoznat"));

        Assert.AreEqual("UserCreate", result.Is<RedirectToActionResult>().ActionName);
    }

    [TestMethod]
    public async Task ConfirmPayment_UnknownUser_SetsErrorMessage()
    {
        var controller = Controller(RoleUser, "nepoznat");

        await Pay(controller);

        Assert.AreEqual("User was not found in the database.", controller.TempData["Greska"]);
    }

    [TestMethod]
    public async Task ConfirmPayment_UserFoundByEmail_CreatesReservationForThatUser()
    {
        SeedAllDevices();

        await Pay(Controller(RoleUser, "identity-1", "u1@test.ba"));

        Assert.AreEqual("u1", Fresh().Rezervacija.Single().KorisnikId);
    }

    [TestMethod]
    public async Task ConfirmPayment_ValidRequest_RedirectsToHome()
    {
        SeedAllDevices();

        var result = await Pay(Controller(RoleUser));

        Assert.AreEqual("Home", result.Is<RedirectToActionResult>().ControllerName);
    }

    [TestMethod]
    [DataRow("PC", 1)]
    [DataRow("PlayStation", 2)]
    [DataRow("XBox", 3)]
    public async Task ConfirmPayment_FreeDevice_ReservesDeviceOfRequestedType(string tip, int ocekivaniId)
    {
        SeedAllDevices();

        await Pay(Controller(RoleUser), tip: tip);

        Assert.AreEqual(ocekivaniId, Fresh().Rezervacija.Single().UredjajId);
    }

    [TestMethod]
    public async Task ConfirmPayment_MultipleHours_CreatesReservationPerHour()
    {
        SeedAllDevices();

        await Pay(Controller(RoleUser), sati: "10,11");

        Assert.AreEqual(2, Fresh().Rezervacija.Count());
    }

    [TestMethod]
    public async Task ConfirmPayment_MultipleHours_SplitsPriceAcrossPayments()
    {
        SeedAllDevices();

        await Pay(Controller(RoleUser), sati: "10,11", ukupno: 10);

        Assert.AreEqual(5.0, Fresh().Placanje.First().Iznos, 0.001);
    }

    // DATA-DRIVEN (CSV): TestData/loyalty.csv
    [TestMethod]
    [DynamicData(nameof(TestDataLoader.LoyaltyRows), typeof(TestDataLoader), DynamicDataSourceType.Method)]
    public async Task ConfirmPayment_WithLoyaltyPoints_ChargesExpectedAmount(int bodovi, int koristi, double ukupno, double ocekivaniIznos, int ocekivaniPreostali)
    {
        SeedAllDevices();
        _seed.SeedLoyalty("u1", bodovi);

        await Pay(Controller(RoleUser), ukupno: ukupno, bodovi: koristi);

        Assert.AreEqual(ocekivaniIznos, Fresh().Placanje.Single().Iznos, 0.001);
    }

    [TestMethod]
    [DynamicData(nameof(TestDataLoader.LoyaltyRows), typeof(TestDataLoader), DynamicDataSourceType.Method)]
    public async Task ConfirmPayment_WithLoyaltyPoints_DeductsExpectedPoints(int bodovi, int koristi, double ukupno, double ocekivaniIznos, int ocekivaniPreostali)
    {
        SeedAllDevices();
        _seed.SeedLoyalty("u1", bodovi);

        await Pay(Controller(RoleUser), ukupno: ukupno, bodovi: koristi);

        Assert.AreEqual(ocekivaniPreostali, Fresh().LoyaltyProgram.Single().UkupniBodovi);
    }

    [TestMethod]
    public async Task ConfirmPayment_UserWithoutLoyaltyRecord_ChargesFullPrice()
    {
        SeedAllDevices();

        await Pay(Controller(RoleUser), ukupno: 10, bodovi: 100);

        Assert.AreEqual(10.0, Fresh().Placanje.Single().Iznos, 0.001);
    }

    [TestMethod]
    public async Task ConfirmPayment_HourInThePast_CreatesNoReservation()
    {
        SeedAllDevices();

        await Pay(Controller(RoleUser), datum: DateTime.Today.AddDays(-1));

        Assert.AreEqual(0, Fresh().Rezervacija.Count());
    }

    [TestMethod]
    public async Task ConfirmPayment_NoFreeDevice_SetsErrorMessage()
    {
        SeedAllDevices();
        var sutra = DateTime.Today.AddDays(1);
        _seed.SeedRezervacija(1, "u1", 1, sutra.AddHours(10), sutra.AddHours(11), StatusRezervacije.Aktivna);
        var controller = Controller(RoleUser);

        await Pay(controller);

        Assert.AreEqual("No available device was found for the selected time.", controller.TempData["Greska"]);
    }

    [TestMethod]
    public async Task ConfirmPayment_NoFreeDevice_RedirectsToUserCreate()
    {
        SeedAllDevices();
        var sutra = DateTime.Today.AddDays(1);
        _seed.SeedRezervacija(1, "u1", 1, sutra.AddHours(10), sutra.AddHours(11), StatusRezervacije.Aktivna);

        var result = await Pay(Controller(RoleUser));

        Assert.AreEqual("UserCreate", result.Is<RedirectToActionResult>().ActionName);
    }

    [TestMethod]
    public async Task ConfirmPayment_UnknownDeviceType_SetsErrorMessage()
    {
        SeedAllDevices();
        var controller = Controller(RoleUser);

        await Pay(controller, tip: "Laptop");

        Assert.AreEqual("No available device was found for the selected time.", controller.TempData["Greska"]);
    }

    [TestMethod]
    public async Task ConfirmPayment_NonNumericHours_ThrowsFormatException()
    {
        SeedAllDevices();
        var controller = Controller(RoleUser);

        await Assert.ThrowsExceptionAsync<FormatException>(() => Pay(controller, sati: "abc"));
    }

    // ===================== CANCEL RESERVATION =====================

    [TestMethod]
    public async Task CancelReservation_ReservationOfAnotherUser_ReturnsNotFound()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u2", 1, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), StatusRezervacije.Aktivna);

        var result = await Controller(RoleUser, "u1").CancelReservation(1);

        result.Is<NotFoundResult>();
    }

    // DATA-DRIVEN (inline): pomak u danima, pocetni status, ocekivani status
    [TestMethod]
    [DataRow(1, StatusRezervacije.Aktivna, StatusRezervacije.Otkazana)]
    [DataRow(-1, StatusRezervacije.Aktivna, StatusRezervacije.Aktivna)]
    [DataRow(1, StatusRezervacije.Zavrsena, StatusRezervacije.Zavrsena)]
    [DataRow(1, StatusRezervacije.Istekla, StatusRezervacije.Istekla)]
    public async Task CancelReservation_OwnReservation_ChangesStatusOnlyWhenActiveAndInFuture(int danaPomaka, StatusRezervacije pocetni, StatusRezervacije ocekivani)
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, DateTime.Now.AddDays(danaPomaka), DateTime.Now.AddDays(danaPomaka).AddHours(1), pocetni);

        await Controller(RoleUser, "u1").CancelReservation(1);

        Assert.AreEqual(ocekivani, Fresh().Rezervacija.Find(1)!.Status);
    }

    [TestMethod]
    public async Task CancelReservation_OwnReservation_RedirectsToIndex()
    {
        SeedBasics();
        _seed.SeedRezervacija(1, "u1", 1, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), StatusRezervacije.Aktivna);

        var result = await Controller(RoleUser, "u1").CancelReservation(1);

        Assert.AreEqual("Index", result.Is<RedirectToActionResult>().ActionName);
    }
}
