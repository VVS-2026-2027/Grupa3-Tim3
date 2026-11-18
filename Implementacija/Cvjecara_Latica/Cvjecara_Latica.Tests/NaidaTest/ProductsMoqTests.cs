
using System.Threading;
using System.Threading.Tasks;
using Cvjecara_Latica.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Cvjecara_Latica.Tests.NaidaTest;

[TestClass]
public class ProductsMoqTests
{
    [TestMethod]
    public async Task SaveChangesAsync_KadaSePozove_VerifikujeSePoziv()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options;

        var mockBaza = new Mock<ApplicationDbContext>(options);

        mockBaza
            .Setup(db => db.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await mockBaza.Object.SaveChangesAsync();

        mockBaza.Verify(
            db => db.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
