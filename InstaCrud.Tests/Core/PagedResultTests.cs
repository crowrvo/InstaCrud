using InstaCrud.Core.Querying;

namespace InstaCrud.Tests.Core;

[TestClass]
public sealed class PagedResultTests {
    [TestMethod]
    public void Deve_Calcular_Total_De_Paginas() {
        var result = new PagedResult<string> {
            Items = ["item"],
            Total = 21,
            Page = 2,
            PageSize = 10
        };

        Assert.AreEqual(3, result.TotalPages);
    }

    [TestMethod]
    public void Deve_Retornar_Zero_Paginas_Quando_Vazio() {
        var result = new PagedResult<string> {
            Items = [],
            Total = 0,
            Page = 1,
            PageSize = 10
        };

        Assert.AreEqual(0, result.TotalPages);
    }
}
