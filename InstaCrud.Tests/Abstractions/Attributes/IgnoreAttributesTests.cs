using InstaCrud.Abstractions.Attributes;
using IgnoreAttribute = InstaCrud.Abstractions.Attributes.IgnoreAttribute;

namespace InstaCrud.Tests.Abstractions.Attributes;

[TestClass]
public class IgnoreAttributesTests {
    private class Usuario {
        [Ignore]
        public string Campo1 { get; set; } = string.Empty;

        [IgnoreInsert]
        public string Campo2 { get; set; } = string.Empty;

        [IgnoreUpdate]
        public string Campo3 { get; set; } = string.Empty;

    }

    [TestMethod]
    public void Deve_Encontrar_Ignore() {
        var propriedade = typeof(Usuario)
            .GetProperty(nameof(Usuario.Campo1));

        Assert.IsTrue(
            propriedade!.IsDefined(typeof(IgnoreAttribute), false));
    }

    [TestMethod]
    public void Deve_Encontrar_IgnoreInsert() {
        var propriedade = typeof(Usuario)
            .GetProperty(nameof(Usuario.Campo2));

        Assert.IsTrue(
            propriedade!.IsDefined(typeof(IgnoreInsertAttribute), false));
    }

    [TestMethod]
    public void Deve_Encontrar_IgnoreUpdate() {
        var propriedade = typeof(Usuario)
            .GetProperty(nameof(Usuario.Campo3));

        Assert.IsTrue(
            propriedade!.IsDefined(typeof(IgnoreUpdateAttribute), false));
    }
}