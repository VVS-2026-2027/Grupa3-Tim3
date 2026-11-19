using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Aplikacija.Models;
using Aplikacija.Tests.Helpers;

namespace Aplikacija.Tests.Models;

[TestClass]
[TestCategory("Model")]
public class RezervacijaTests
{
    private Rezervacija _rezervacija = null!;

    [TestInitialize]
    public void Init()
    {
        _rezervacija = new Rezervacija
        {
            Id = 1,
            VrijemeOd = new DateTime(2030, 1, 1, 10, 0, 0),
            VrijemeDo = new DateTime(2030, 1, 1, 11, 0, 0),
            KorisnikId = "u1",
            UredjajId = 1
        };
    }

    [TestCleanup]
    public void Cleanup() => _rezervacija = null!;

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(42)]
    [DataRow(int.MaxValue)]
    public void GetId_ReturnsAssignedId(int id)
    {
        _rezervacija.Id = id;

        Assert.AreEqual(id, _rezervacija.GetId());
    }

    [TestMethod]
    public void NewRezervacija_DefaultStatus_IsAktivna()
    {
        var nova = new Rezervacija();

        Assert.AreEqual(StatusRezervacije.Aktivna, nova.Status);
    }

    [TestMethod]
    public void NewRezervacija_Placanje_IsNull()
    {
        Assert.IsNull(_rezervacija.Placanje);
    }

    [TestMethod]
    public void Validate_ValidReservation_HasNoErrors()
    {
        Assert.IsTrue(ValidationHelper.IsValid(_rezervacija));
    }

    [TestMethod]
    [DataRow(2019, false)]
    [DataRow(2020, true)]
    [DataRow(2100, true)]
    [DataRow(2101, false)]
    public void Validate_VrijemeOdYear_RespectsAllowedRange(int year, bool expectedValid)
    {
        _rezervacija.VrijemeOd = new DateTime(year, 6, 1);

        Assert.AreEqual(expectedValid, ValidationHelper.IsValid(_rezervacija));
    }

    [TestMethod]
    [DataRow(2019, false)]
    [DataRow(2050, true)]
    [DataRow(2101, false)]
    public void Validate_VrijemeDoYear_RespectsAllowedRange(int year, bool expectedValid)
    {
        _rezervacija.VrijemeDo = new DateTime(year, 6, 1);

        Assert.AreEqual(expectedValid, ValidationHelper.IsValid(_rezervacija));
    }

    [TestMethod]
    public void Validate_DefaultDates_AreInvalid()
    {
        var prazna = new Rezervacija();

        Assert.IsFalse(ValidationHelper.IsValid(prazna));
    }

    [TestMethod]
    public void Validate_VrijemeOdOutOfRange_ReturnsExpectedMessage()
    {
        _rezervacija.VrijemeOd = new DateTime(2019, 1, 1);

        var messages = ValidationHelper.Validate(_rezervacija).Select(r => r.ErrorMessage).ToList();

        CollectionAssert.Contains(messages, "Start time must be between 2020 and 2100!");
    }

    [TestMethod]
    public void Validate_UndefinedStatusValue_IsInvalid()
    {
        _rezervacija.Status = (StatusRezervacije)99;

        Assert.IsFalse(ValidationHelper.IsValid(_rezervacija));
    }

    [TestMethod]
    [DataRow(StatusRezervacije.Aktivna)]
    [DataRow(StatusRezervacije.Otkazana)]
    [DataRow(StatusRezervacije.Istekla)]
    [DataRow(StatusRezervacije.Zavrsena)]
    public void Validate_DefinedStatusValue_IsValid(StatusRezervacije status)
    {
        _rezervacija.Status = status;

        Assert.IsTrue(ValidationHelper.IsValid(_rezervacija));
    }
}

[TestClass]
[TestCategory("Model")]
public class StatusRezervacijeTests
{
    [TestMethod]
    [DataRow(StatusRezervacije.Aktivna, 0)]
    [DataRow(StatusRezervacije.Otkazana, 1)]
    [DataRow(StatusRezervacije.Istekla, 2)]
    [DataRow(StatusRezervacije.Zavrsena, 3)]
    public void Value_HasExpectedUnderlyingInteger(StatusRezervacije status, int expected)
    {
        Assert.AreEqual(expected, (int)status);
    }

    [TestMethod]
    [DataRow(nameof(StatusRezervacije.Aktivna), "Active")]
    [DataRow(nameof(StatusRezervacije.Otkazana), "Canceled")]
    [DataRow(nameof(StatusRezervacije.Istekla), "Expired")]
    [DataRow(nameof(StatusRezervacije.Zavrsena), "Completed")]
    public void DisplayAttribute_ReturnsEnglishName(string memberName, string expectedDisplay)
    {
        var display = typeof(StatusRezervacije).GetField(memberName)!.GetCustomAttribute<DisplayAttribute>()!;

        Assert.AreEqual(expectedDisplay, display.Name);
    }

    [TestMethod]
    public void Enum_HasExactlyFourValues()
    {
        Assert.AreEqual(4, Enum.GetValues<StatusRezervacije>().Length);
    }
}
