using Aplikacija.Models;

namespace Aplikacija.Tests.Services;

[TestClass, TestCategory("Model")]
public class DeviceServiceTestovi
{
    [Owner("Adnan"), TestMethod]
    public void Konstruktor_NoviObjekat_IdJeNula()
    {
          
        var servis = new DeviceService();
          
        Assert.AreEqual(0, servis.Id);
    }

    [Owner("Adnan"), TestMethod]
    [DataRow(0), DataRow(12), DataRow(-1), DataRow(int.MaxValue)]
    public void Id_Postavljanje_CuvaVrijednost(int id)
    {
         
        var servis = new DeviceService();
           
        servis.Id = id;
          
        Assert.AreEqual(id, servis.Id);
    }
}
