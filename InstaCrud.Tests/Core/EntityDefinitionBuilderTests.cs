using InstaCrud.Abstractions.Attributes;
using InstaCrud.Handler;

namespace InstaCrud.Tests.Core;

[TestClass]
public sealed class EntityDefinitionBuilderTests {
    private readonly EntityDefinitionBuilder _builder = new();

    [Crud("usuarios")]
    [Table("TAB_USUARIO")]
    private sealed class Usuario {
        [Key]
        [DatabaseGenerated]
        [Column("COD_USUARIO")]
        public int Id { get; set; }

        [IgnoreUpdate]
        public string Nome { get; set; } = string.Empty;

        public string this[int index] => index.ToString();
    }

    private sealed class EntidadeSemCrud;

    [TestMethod]
    public void Deve_Construir_Definicao_Com_Mapeamentos() {
        var definition = _builder.Build(typeof(Usuario));

        Assert.AreEqual("usuarios", definition.RouteName);
        Assert.AreEqual("TAB_USUARIO", definition.TableName);
        Assert.HasCount(2, definition.Properties);
        Assert.HasCount(1, definition.KeyProperties);

        var key = definition.KeyProperties.Single();
        Assert.AreEqual("COD_USUARIO", key.ColumnName);
        Assert.IsTrue(key.IsDatabaseGenerated);

        var name = definition.Properties.Single(x => x.PropertyName == nameof(Usuario.Nome));
        Assert.IsTrue(name.IgnoreUpdate);
        Assert.IsTrue(name.IgnorePatch);
    }

    [TestMethod]
    public void Deve_Compilar_Acessores_Da_Propriedade() {
        var definition = _builder.Build(typeof(Usuario));
        var property = definition.Properties.Single(x => x.PropertyName == nameof(Usuario.Nome));
        var entity = new Usuario();

        property.Setter!(entity, "Maria");

        Assert.AreEqual("Maria", property.Getter!(entity));
    }

    [TestMethod]
    public void Deve_Rejeitar_Entidade_Sem_Crud() {
        Assert.ThrowsExactly<InvalidOperationException>(
            () => _builder.Build(typeof(EntidadeSemCrud)));
    }
}
