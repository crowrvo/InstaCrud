using InstaCrud.Abstractions.Attributes;

namespace InstaCrud.Tests.Abstractions.Attributes;

[TestClass]
public class CrudAttributeTests {
    [Crud]
    private class SemRota {
    }

    [Crud("usuarios")]
    private class ComRota {
    }

    [TestMethod]
    public void Deve_Encontrar_CrudAttribute() {
        var atributo = typeof(SemRota)
            .GetCustomAttributes(typeof(CrudAttribute), false)
            .FirstOrDefault();

        Assert.IsNotNull(atributo);
    }

    [TestMethod]
    public void Deve_Ler_Rota_Configurada() {
        var atributo = typeof(ComRota)
            .GetCustomAttributes(typeof(CrudAttribute), false)
            .Cast<CrudAttribute>()
            .Single();

        Assert.AreEqual("usuarios", atributo.RouteName);
    }
}