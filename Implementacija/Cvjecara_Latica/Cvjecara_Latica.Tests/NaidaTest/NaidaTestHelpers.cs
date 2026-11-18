
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Cvjecara_Latica.Data;
using Cvjecara_Latica.Models;
using Cvjecara_Latica.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cvjecara_Latica.Tests.NaidaTest;

internal static class NaidaTestHelpers
{
    public static ApplicationDbContext Baza()
        => TestDbContextFactory.Kreiraj();

    public static Product Proizvod(int id = 1) => new()
    {
        ProductID = id,
        Name = "Ruza",
        ImageUrl = "/img/ruza.jpg",
        Price = 15.0,
        FlowerType = "Rose",
        Stock = 20,
        Category = "Cvijece",
        Description = "Kvalitetna crvena ruza",
        productType = "Flower",
        Color = "Red"
    };

    public static ProductOrder Stavka(int id = 1) => new()
    {
        ProductOrderID = id,
        ProductID = 1,
        OrderID = 1,
        ProductQuantity = 2
    };

    public static Report Izvjestaj(
        int id = 1,
        string osoba = "u1") => new()
    {
        ReportID = id,
        PersonID = osoba,
        Content = "Sedmicni izvjestaj",
        Date = new DateTime(2026, 11, 18)
    };


public static Person Osoba(string id = "u1")
{
    return new Person
    {
        Id = id,
        UserName = id,
        NormalizedUserName = id.ToUpperInvariant(),
        Email = id + "@test.com",
        NormalizedEmail = (id + "@test.com").ToUpperInvariant(),
        FirstName = "Test",
        LastName = "Korisnik",
        HomeAdress = "Test Ulica 10"
    };
}



    public static void Korisnik(
        Controller controller,
        string id,
        bool administrator)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id)
        };

        if (administrator)
        {
            claims.Add(
                new Claim(ClaimTypes.Role, "Administrator")
            );
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, "Test")
                )
            }
        };
    }
}
