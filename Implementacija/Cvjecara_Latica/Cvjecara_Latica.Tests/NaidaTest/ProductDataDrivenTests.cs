
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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

    [TestMethod]
    public void Proizvod_CSV_PodaciSeIspravnoUcitavaju()
    {
        string putanja = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
              "Naida",
               "proizvodi.csv");

        Assert.IsTrue(File.Exists(putanja),
            "CSV datoteka nije pronadjena.");

        var redovi = File.ReadAllLines(putanja)
            .Skip(1)
            .Where(red => !string.IsNullOrWhiteSpace(red))
            .ToList();

        Assert.IsTrue(redovi.Count > 0);

        foreach (var red in redovi)
        {
            var kolone = red.Split(',');

            var proizvod = NaidaTestHelpers.Proizvod();
            proizvod.Name = kolone[0];
            proizvod.Price = double.Parse(
                kolone[1],
                System.Globalization.CultureInfo.InvariantCulture);
            proizvod.Stock = int.Parse(kolone[2]);

            Assert.IsFalse(string.IsNullOrWhiteSpace(proizvod.Name));
            Assert.IsTrue(proizvod.Price > 0);
            Assert.IsTrue(proizvod.Stock >= 0);
        }
    }

    [TestMethod]
    public void Proizvod_XML_PodaciSeIspravnoUcitavaju()
    {
        string putanja = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
             "Naida",
             "proizvodi.xml");

        Assert.IsTrue(File.Exists(putanja),
            "XML datoteka nije pronadjena.");

        var dokument = XDocument.Load(putanja);
        var proizvodi = dokument.Descendants("Proizvod").ToList();

        Assert.IsTrue(proizvodi.Count > 0);

        foreach (var element in proizvodi)
        {
            var proizvod = NaidaTestHelpers.Proizvod();

            proizvod.Name = (string)element.Element("Naziv")!;
            proizvod.Price = double.Parse(
                (string)element.Element("Cijena")!,
                System.Globalization.CultureInfo.InvariantCulture);
            proizvod.Stock = int.Parse(
                (string)element.Element("Zaliha")!);

            Assert.IsFalse(string.IsNullOrWhiteSpace(proizvod.Name));
            Assert.IsTrue(proizvod.Price > 0);
            Assert.IsTrue(proizvod.Stock >= 0);
        }
    }
}
