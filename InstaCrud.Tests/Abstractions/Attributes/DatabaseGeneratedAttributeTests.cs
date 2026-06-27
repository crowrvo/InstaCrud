using InstaCrud.Abstractions.Attributes;

namespace InstaCrud.Tests.Abstractions.Attributes;

[TestClass]
public class DatabaseGeneratedAttributeTests {
    private class Usuario {
        [DatabaseGenerated]
        public int Id { get; set; }
    }

    [TestMethod]
    public void Deve_Encontrar_DatabaseGenerated() {
        var propriedade = typeof(Usuario)
            .GetProperty(nameof(Usuario.Id));

        Assert.IsTrue(
            propriedade!.IsDefined(
                typeof(DatabaseGeneratedAttribute),
                false));
    }
}