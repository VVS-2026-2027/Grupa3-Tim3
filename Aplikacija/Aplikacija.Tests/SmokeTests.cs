namespace Aplikacija.Tests;

[TestClass, TestCategory("Smoke")]
public sealed class SmokeTests
{
    [TestMethod]
    [DataRow("OpisiKvara.csv")]
    [DataRow("Statusi.json")]
    public void TestData_KopiranjePriBuildanju_PotrebniFajlPostoji(string naziv)
    {
         
        string putanja = Path.Combine(AppContext.BaseDirectory, "TestData", naziv);

           
        bool postoji = File.Exists(putanja);

          
        Assert.IsTrue(postoji, "Testni podatak nije kopiran u izlazni TestData folder: " + naziv);
    }
}
