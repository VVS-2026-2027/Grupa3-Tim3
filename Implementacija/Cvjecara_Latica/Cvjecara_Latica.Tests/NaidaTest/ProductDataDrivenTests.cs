
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Cvjecara_Latica.Models;

namespace Cvjecara_Latica.Tests.NaidaTest;

[TestClass]
public class ProductDataDrivenTests
{
    [TestMethod]
    [DataRow("Ruza", 15.0, 20)]
    [DataRow("Tulipan", 8.0, 10)]
    [DataRow("Orhideja", 30.0, 5)]
    public void Proizvod_PodaciIzDataRow_Ispravni(
        string naziv, double cijena, int zaliha)
    {
        var proizvod = NaidaTestHelpers.Proizvod();

        proizvod.Name = naziv;
        proizvod.Price = cijena;
        proizvod.Stock = zaliha;

        Assert.AreEqual(naziv, proizvod.Name);
        Assert.AreEqual(cijena, proizvod.Price);
        Assert.AreEqual(zaliha, proizvod.Stock);
    }

    private static string Putanja(string fajl) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "Naida", fajl);

    public static IEnumerable<object[]> ProizvodiIzCsv() =>
        File.ReadAllLines(Putanja("proizvodi.csv"))
            .Skip(1)
            .Where(red => !string.IsNullOrWhiteSpace(red))
            .Select(red => red.Split(','))
            .Select(k => new object[] { k[0], double.Parse(k[1], CultureInfo.InvariantCulture), int.Parse(k[2]) });

    public static IEnumerable<object[]> ProizvodiIzXml() =>
        XDocument.Load(Putanja("proizvodi.xml"))
            .Descendants("Proizvod")
            .Select(e => new object[]
            {
                (string)e.Element("Naziv")!,
                double.Parse((string)e.Element("Cijena")!, CultureInfo.InvariantCulture),
                int.Parse((string)e.Element("Zaliha")!)
            });

    [TestMethod]
    [DynamicData(nameof(ProizvodiIzCsv))]
    public void Proizvod_PodaciIzCsv_ProlazeValidaciju(string naziv, double cijena, int zaliha)
    {
        var proizvod = NaidaTestHelpers.Proizvod();
        proizvod.Name = naziv;
        proizvod.Price = cijena;
        proizvod.Stock = zaliha;
        var greske = new List<ValidationResult>();

        var validan = Validator.TryValidateObject(proizvod, new ValidationContext(proizvod), greske, true);

        Assert.IsTrue(validan);
        Assert.IsEmpty(greske);
    }

    [TestMethod]
    [DynamicData(nameof(ProizvodiIzXml))]
    public void Proizvod_PodaciIzXml_ProlazeValidaciju(string naziv, double cijena, int zaliha)
    {
        var proizvod = NaidaTestHelpers.Proizvod();
        proizvod.Name = naziv;
        proizvod.Price = cijena;
        proizvod.Stock = zaliha;
        var greske = new List<ValidationResult>();

        var validan = Validator.TryValidateObject(proizvod, new ValidationContext(proizvod), greske, true);

        Assert.IsTrue(validan);
        Assert.IsEmpty(greske);
    }
}
