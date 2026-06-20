using InstaCrud.Decorators;

namespace InstaCrud.Tests.Abstractions.Attributes;

[TestClass]
public class ColumnAttributeTests {
    private class Usuario {
        [Column("COD_USUARIO")]
        public int Id { get; set; }
    }

    [TestMethod]
    public void Deve_Ler_Nome_Da_Coluna() {
        var propriedade = typeof(Usuario)
            .GetProperty(nameof(Usuario.Id));

        var atributo = propriedade!
            .GetCustomAttributes(typeof(ColumnAttribute), false)
            .Cast<ColumnAttribute>()
            .Single();

        Assert.AreEqual("COD_USUARIO", atributo.Name);
    }
}