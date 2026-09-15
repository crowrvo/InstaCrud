using InstaCrud.Abstractions.Attributes;

namespace InstaCrud.Tests.Abstractions.Attributes;

[TestClass]
public class KeyAttributeTests {
    private class Usuario {
        [Key]
        public int Id { get; set; }
    }

    [TestMethod]
    public void Deve_Identificar_Chave_Primaria() {
        var propriedade = typeof(Usuario)
            .GetProperty(nameof(Usuario.Id));

        var possuiAtributo =
            propriedade!.IsDefined(typeof(KeyAttribute), false);

        Assert.IsTrue(possuiAtributo);
    }
}
