using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Xml.Linq;

namespace Aplikacija.Tests.Helpers;

public static class TestFileReader
{
    private static string Path(string f) =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", f);

    public static IEnumerable<string[]> ReadCsv(string f) =>
        File.ReadAllLines(Path(f)).Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Split(','));

    public static IEnumerable<T> ReadJson<T>(string f) =>
        JsonSerializer.Deserialize<List<T>>(File.ReadAllText(Path(f)),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    public static IEnumerable<XElement> ReadXml(string f) =>
        XDocument.Load(Path(f)).Root!.Elements();
}