using System.Text.Json;
using Aplikacija.Models;

namespace Aplikacija.Tests.Models;

[TestClass]
[TestCategory("Model")]
public class PandaScoreMatchTests
{
    [TestMethod]
    public void NewInstance_AllPropertiesAreNull()
    {
        var match = new PandaScoreMatch();

        Assert.IsNull(match.name);
        Assert.IsNull(match.begin_at);
        Assert.IsNull(match.videogame);
        Assert.IsNull(match.league);
    }

    [TestMethod]
    public void Properties_AreStoredCorrectly()
    {
        var match = new PandaScoreMatch
        {
            name = "A vs B",
            begin_at = "2030-01-01T10:00:00",
            videogame = new PandaScoreVideogame { name = "CS2" },
            league = new PandaScoreLeague { name = "ESL" }
        };

        Assert.AreEqual("A vs B", match.name);
        Assert.AreEqual("2030-01-01T10:00:00", match.begin_at);
        Assert.AreEqual("CS2", match.videogame!.name);
        Assert.AreEqual("ESL", match.league!.name);
    }

    // DATA-DRIVEN (inline DataRow): razliciti oblici JSON odgovora
    [TestMethod]
    [DataRow("""{"name":"A","begin_at":"2030-01-01T10:00:00"}""", "A", "2030-01-01T10:00:00")]
    [DataRow("""{"name":null,"begin_at":null}""", null, null)]
    [DataRow("""{}""", null, null)]
    [DataRow("""{"name":"B","unknown_field":123}""", "B", null)]
    public void Deserialize_Json_MapsNameAndBeginAt(string json, string? expectedName, string? expectedBeginAt)
    {
        var match = JsonSerializer.Deserialize<PandaScoreMatch>(json)!;

        Assert.AreEqual(expectedName, match.name);
        Assert.AreEqual(expectedBeginAt, match.begin_at);
    }

    [TestMethod]
    public void Deserialize_JsonWithNestedObjects_MapsVideogameAndLeague()
    {
        const string json = """{"videogame":{"name":"Valorant"},"league":{"name":"VCT"}}""";

        var match = JsonSerializer.Deserialize<PandaScoreMatch>(json)!;

        Assert.AreEqual("Valorant", match.videogame!.name);
        Assert.AreEqual("VCT", match.league!.name);
    }

    [TestMethod]
    public void Deserialize_JsonArray_ReturnsAllMatches()
    {
        const string json = """[{"name":"A"},{"name":"B"},{"name":"C"}]""";

        var matches = JsonSerializer.Deserialize<List<PandaScoreMatch>>(json)!;

        Assert.AreEqual(3, matches.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(JsonException), AllowDerivedTypes = true)]
    public void Deserialize_MalformedJson_ThrowsJsonException()
    {
        JsonSerializer.Deserialize<PandaScoreMatch>("{ not valid json");
    }
}
