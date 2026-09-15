using InstaCrud.Abstractions.Attributes;
using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Handler;
using IgnoreAttribute = InstaCrud.Abstractions.Attributes.IgnoreAttribute;

namespace InstaCrud.Tests.Core;

[TestClass]
public sealed class EntityCommandFactoryTests {
    private readonly EntityCommandFactory _factory = new(
        EntityRegistry.FromTypes(typeof(Usuario), typeof(SemChave)));

    [Crud]
    [Table("dbo.USUARIO")]
    private sealed class Usuario {
        [Key]
        [DatabaseGenerated]
        [Column("ID")]
        public int Id { get; set; }

        [Column("NOME")]
        public string Nome { get; set; } = string.Empty;

        [IgnoreUpdate]
        public DateTime CriadoEm { get; set; }

        [IgnoreInsert]
        public DateTime? UltimoAcesso { get; set; }

        [IgnoreAttribute]
        public string SegredoTemporario { get; set; } = string.Empty;
    }

    [Crud]
    private sealed class SemChave {
        public string Nome { get; set; } = string.Empty;
    }

    [TestMethod]
    public void Deve_Criar_Insert_Apenas_Com_Propriedades_Permitidas() {
        var entity = CreateUsuario();

        var command = _factory.CreateInsert(entity);

        Assert.AreEqual(CrudOperationType.Insert, command.OperationType);
        Assert.AreEqual("dbo.USUARIO", command.TableName);
        CollectionAssert.AreEqual(
            new[] { "NOME", nameof(Usuario.CriadoEm) },
            command.Fields.Select(x => x.ColumnName).ToArray());
        Assert.AreEqual("ID", command.ReturningFields.Single().ColumnName);
        Assert.AreEqual("Maria", command.Fields.First().Value);
    }

    [TestMethod]
    public void Deve_Criar_Update_Com_Chave_E_Campos_Permitidos() {
        var command = _factory.CreateUpdate(CreateUsuario());

        CollectionAssert.AreEqual(
            new[] { "NOME", nameof(Usuario.UltimoAcesso) },
            command.Fields.Select(x => x.ColumnName).ToArray());
        Assert.HasCount(1, command.Filters);
        Assert.AreEqual("ID", command.Filters.Single().ColumnName);
        Assert.AreEqual(42, command.Filters.Single().Value);
    }

    [TestMethod]
    public void Deve_Criar_Patch_Somente_Com_Propriedades_Solicitadas() {
        var command = _factory.CreatePatch(CreateUsuario(), [nameof(Usuario.Nome)]);

        Assert.AreEqual(CrudOperationType.Patch, command.OperationType);
        Assert.HasCount(1, command.Fields);
        Assert.AreEqual("NOME", command.Fields.Single().ColumnName);
        Assert.HasCount(1, command.Filters);
    }

    [TestMethod]
    public void Deve_Rejeitar_Propriedade_Protegida_No_Patch() {
        Assert.ThrowsExactly<ArgumentException>(() =>
            _factory.CreatePatch(CreateUsuario(), [nameof(Usuario.CriadoEm)]));
    }

    [TestMethod]
    public void Deve_Criar_Delete_Usando_A_Chave() {
        var command = _factory.CreateDelete(CreateUsuario());

        Assert.AreEqual(CrudOperationType.Delete, command.OperationType);
        Assert.IsEmpty(command.Fields);
        Assert.AreEqual(42, command.Filters.Single().Value);
    }

    [TestMethod]
    public void Deve_Criar_Select_Com_Colunas_Mapeadas() {
        var command = _factory.CreateSelect<Usuario>();

        CollectionAssert.AreEqual(
            new[] { "ID", "NOME", nameof(Usuario.CriadoEm), nameof(Usuario.UltimoAcesso) },
            command.Fields.Select(x => x.ColumnName).ToArray());
    }

    [TestMethod]
    public void Deve_Exigir_Chave_Para_Update_E_Delete() {
        var entity = new SemChave { Nome = "teste" };

        Assert.ThrowsExactly<InvalidOperationException>(() => _factory.CreateUpdate(entity));
        Assert.ThrowsExactly<InvalidOperationException>(() => _factory.CreateDelete(entity));
    }

    private static Usuario CreateUsuario() => new() {
        Id = 42,
        Nome = "Maria",
        CriadoEm = new DateTime(2026, 1, 2),
        UltimoAcesso = new DateTime(2026, 2, 3),
        SegredoTemporario = "não persistir"
    };
}
