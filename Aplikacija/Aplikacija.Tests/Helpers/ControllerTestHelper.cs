using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Aplikacija.Tests.Helpers;

public static class ControllerTestHelper
{
    public static T WithUser<T>(this T controller, string userId, string role, string? email = null) where T : Controller
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, email ?? userId + "@test.ba"),
            new(ClaimTypes.Role, role)
        };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, new NullTempDataProvider());
        return controller;
    }

    /// <summary>Provjerava tip rezultata i vraca ga castanog.</summary>
    public static T Is<T>(this IActionResult result) where T : class
    {
        Assert.IsInstanceOfType(result, typeof(T));
        return (T)result;
    }

    public static T ModelOf<T>(this IActionResult result) where T : class
        => (T)result.Is<ViewResult>().Model!;

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
