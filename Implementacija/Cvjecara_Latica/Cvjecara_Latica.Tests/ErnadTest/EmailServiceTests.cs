using System.Net.Mail;
using Cvjecara_Latica.Services;
using Moq;

namespace Cvjecara_Latica.Tests.ErnadTest;

/// <summary>
/// Unit testovi za IEmailService / EmailService (Clan 3).
/// ZAMJENSKI OBJEKAT: Mock&lt;IConfiguration&gt; zamjenjuje stvarni appsettings.json
/// (stvarni SMTP podaci se ne koriste i nijedan pravi mail se ne salje).
/// DATA-DRIVEN: TestData/Ernad/email_konfiguracije.xml.
/// Napomena: "nedostupan server" testovi koriste 127.0.0.1:1 (lokalni port koji odbija vezu),
/// pa nije potrebna mreza ni pravi SMTP server.
/// </summary>
[TestClass]
[TestCategory("EmailService")]
public class EmailServiceTests
{
    private const string Kupac = "kupac@test.ba";
    private const string Naslov = "Naslov poruke";
    private const string Tijelo = "<p>Tekst poruke</p>";

    private static EmailService Servis(string? server, string? port, string? posiljalac)
        => new(ErnadPomocnici.NapraviKonfiguraciju(server, port, posiljalac).Object);

    private static EmailService ServisSaNedostupnimServerom()
        => Servis("127.0.0.1", "1", "latica@test.ba");

    // ---------- IEmailService ----------

    [Owner("Ernad"), TestMethod]
    public void EmailService_Kreiran_ImplementiraIEmailService()
    {
        // Arrange + Act
        object servis = Servis("127.0.0.1", "587", "latica@test.ba");
        // Assert
        Assert.IsInstanceOfType<IEmailService>(servis);
    }

    // ---------- SendEmailAsync: izuzeci (DATA-DRIVEN, XML) ----------

    [Owner("Ernad"), TestMethod]
    [DynamicData(nameof(ErnadPomocnici.EmailKonfiguracije), typeof(ErnadPomocnici), DynamicDataSourceType.Method)]
    public async Task SendEmailAsync_NeispravnaKonfiguracijaIliAdresa_BacaOcekivaniIzuzetak(
        string naziv, string? server, string? port, string? posiljalac, string primalac, Type ocekivaniTip)
    {
        // Arrange
        var servis = Servis(server, port, posiljalac);
        // Act
        var izuzetak = await Assert.ThrowsAsync<Exception>(() => servis.SendEmailAsync(primalac, Naslov, Tijelo));
        // Assert
        Assert.IsInstanceOfType(izuzetak, ocekivaniTip, naziv);
    }

    // ---------- SendEmailAsync: osnovna funkcionalnost ----------

    [Owner("Ernad"), TestMethod]
    public async Task SendEmailAsync_SmtpServerNedostupan_BacaSmtpException()
    {
        // Arrange
        var servis = ServisSaNedostupnimServerom();
        // Act + Assert
        await Assert.ThrowsAsync<SmtpException>(() => servis.SendEmailAsync(Kupac, Naslov, Tijelo));
    }

    [Owner("Ernad"), TestMethod]
    [DataRow("EmailSettings:SmtpServer")]
    [DataRow("EmailSettings:SmtpPort")]
    [DataRow("EmailSettings:Username")]
    [DataRow("EmailSettings:Password")]
    [DataRow("EmailSettings:SenderEmail")]
    [DataRow("EmailSettings:SenderName")]
    public async Task SendEmailAsync_Pozvan_CitaPostavkuIzKonfiguracije(string kljuc)
    {
        // Arrange
        var konfiguracija = ErnadPomocnici.NapraviKonfiguraciju("127.0.0.1", "1", "latica@test.ba");
        var servis = new EmailService(konfiguracija.Object);
        // Act
        await Assert.ThrowsAsync<SmtpException>(() => servis.SendEmailAsync(Kupac, Naslov, Tijelo));
        // Assert
        konfiguracija.Verify(c => c[kljuc], Times.Once());
    }

    // ---------- SendReminder ----------

    [Owner("Ernad"), TestMethod]
    public async Task SendReminder_NedostajePort_BacaArgumentNullException()
    {
        // Arrange
        var servis = Servis("127.0.0.1", null, "latica@test.ba");
        // Act + Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => servis.SendReminder(Kupac));
    }

    [Owner("Ernad"), TestMethod]
    public async Task SendReminder_SmtpServerNedostupan_BacaSmtpException()
    {
        // Arrange
        var servis = ServisSaNedostupnimServerom();
        // Act + Assert
        await Assert.ThrowsAsync<SmtpException>(() => servis.SendReminder(Kupac));
    }

    // ---------- SendDeactivationNotice ----------

    [Owner("Ernad"), TestMethod]
    public async Task SendDeactivationNotice_NedostajePort_BacaArgumentNullException()
    {
        // Arrange
        var servis = Servis("127.0.0.1", null, "latica@test.ba");
        // Act + Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => servis.SendDeactivationNotice(Kupac));
    }

    [Owner("Ernad"), TestMethod]
    public async Task SendDeactivationNotice_SmtpServerNedostupan_BacaSmtpException()
    {
        // Arrange
        var servis = ServisSaNedostupnimServerom();
        // Act + Assert
        await Assert.ThrowsAsync<SmtpException>(() => servis.SendDeactivationNotice(Kupac));
    }
}
