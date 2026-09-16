using InstaCrud.Abstractions.Attributes;
using InstaCrud.Handler;
using InstaCrud.Exceptions;
using IgnoreAttribute = InstaCrud.Abstractions.Attributes.IgnoreAttribute;

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

    [Crud("")]
    private sealed class EntidadeComRotaVazia;

    [Crud]
    [Table(" ")]
    private sealed class EntidadeComTabelaVazia;

    [Crud]
    private sealed class EntidadeComColunasDuplicadas {
        [Column("VALOR")]
        public int Primeiro { get; set; }

        [Column("valor")]
        public int Segundo { get; set; }
    }

    [Crud]
    private sealed class EntidadeComChaveIgnorada {
        [Key]
        [IgnoreAttribute]
        public int Id { get; set; }
    }

    [Crud]
    private sealed class EntidadeComGeradoSomenteLeitura {
        [Key]
        [DatabaseGenerated]
        public int Id { get; }
    }

    [Crud]
    private sealed class EntidadeComPropriedadeSomenteEscrita {
        public string Nome {
            set { }
        }
    }

    [Crud]
    private abstract class EntidadeAbstrata;

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
        Assert.ThrowsExactly<CrudConfigurationException>(
            () => _builder.Build(typeof(EntidadeSemCrud)));
    }

    [TestMethod]
    public void Deve_Rejeitar_Rota_E_Tabela_Vazias() {
        Assert.ThrowsExactly<CrudConfigurationException>(
            () => _builder.Build(typeof(EntidadeComRotaVazia)));
        Assert.ThrowsExactly<CrudConfigurationException>(
            () => _builder.Build(typeof(EntidadeComTabelaVazia)));
    }

    [TestMethod]
    public void Deve_Rejeitar_Colunas_Duplicadas() {
        var exception = Assert.ThrowsExactly<CrudConfigurationException>(
            () => _builder.Build(typeof(EntidadeComColunasDuplicadas)));

        Assert.AreEqual(typeof(EntidadeComColunasDuplicadas), exception.EntityType);
    }

    [TestMethod]
    public void Deve_Rejeitar_Chave_Ignorada() {
        var exception = Assert.ThrowsExactly<CrudConfigurationException>(
            () => _builder.Build(typeof(EntidadeComChaveIgnorada)));

        Assert.AreEqual(nameof(EntidadeComChaveIgnorada.Id), exception.PropertyName);
    }

    [TestMethod]
    public void Deve_Rejeitar_Propriedade_Gerada_Sem_Setter() {
        Assert.ThrowsExactly<CrudConfigurationException>(
            () => _builder.Build(typeof(EntidadeComGeradoSomenteLeitura)));
    }

    [TestMethod]
    public void Deve_Rejeitar_Propriedade_Persistida_Sem_Getter() {
        Assert.ThrowsExactly<CrudConfigurationException>(
            () => _builder.Build(typeof(EntidadeComPropriedadeSomenteEscrita)));
    }

    [TestMethod]
    public void Deve_Rejeitar_Entidade_Abstrata() {
        Assert.ThrowsExactly<CrudConfigurationException>(
            () => _builder.Build(typeof(EntidadeAbstrata)));
    }
}
