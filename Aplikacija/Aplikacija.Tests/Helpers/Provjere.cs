using Microsoft.AspNetCore.Mvc;

namespace Aplikacija.Tests.Helpers;

public static class Provjere
{
    public static ViewResult Prikaz(IActionResult rezultat)
    {
        Assert.IsInstanceOfType<ViewResult>(rezultat);
        return (ViewResult)rezultat;
    }

    public static T ModelPrikaza<T>(IActionResult rezultat)
    {
        var prikaz = Prikaz(rezultat);
        Assert.IsInstanceOfType<T>(prikaz.Model);
        return (T)prikaz.Model!;
    }

    public static void Preusmjerenje(IActionResult rezultat, string akcija = "Index")
    {
        Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        Assert.AreEqual(akcija, ((RedirectToActionResult)rezultat).ActionName);
    }
}
