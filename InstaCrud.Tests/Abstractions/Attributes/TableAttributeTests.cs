using InstaCrud.Abstractions.Attributes;

namespace InstaCrud.Tests.Abstractions.Attributes;

[TestClass]
public class TableAttributeTests {
    [Table("TAB_USUARIO")]
    private class Usuario {
    }

    [TestMethod]
    public void Deve_Ler_Nome_Da_Tabela() {
        var atributo = typeof(Usuario)
            .GetCustomAttributes(typeof(TableAttribute), false)
            .Cast<TableAttribute>()
            .Single();

        Assert.AreEqual("TAB_USUARIO", atributo.Name);
    }
}