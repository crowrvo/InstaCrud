using InstaCrud.Abstractions.Attributes;
using InstaCrud.Abstractions.CrudCommand;
using InstaCrud.Handler;
using InstaCrud.Core.Querying;
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
    public void Deve_Criar_Consulta_Tipada_Com_Mapeamentos() {
        var query = new CrudQuery<Usuario>()
            .Select(x => x.Id, x => x.Nome)
            .Where(x => x.Nome, CrudFilterOperator.Like, "Mar%")
            .OrderByDescending(x => x.Nome)
            .Page(2, 10);

        var command = _factory.CreateSelect(query);

        CollectionAssert.AreEqual(
            new[] { "ID", "NOME" },
            command.Fields.Select(x => x.ColumnName).ToArray());
        Assert.AreEqual("NOME", command.Filters.Single().ColumnName);
        Assert.AreEqual(CrudFilterOperator.Like, command.Filters.Single().Operator);
        Assert.AreEqual("NOME", command.Sorts.Single().ColumnName);
        Assert.IsTrue(command.Sorts.Single().Descending);
        Assert.AreEqual(2, command.Pagination!.Page);
        Assert.AreEqual(10, command.Pagination.PageSize);
    }

    [TestMethod]
    public void Deve_Rejeitar_Propriedade_Ignorada_Na_Consulta() {
        var query = new CrudQuery<Usuario>()
            .Where(x => x.SegredoTemporario, CrudFilterOperator.Equal, "segredo");

        Assert.ThrowsExactly<ArgumentException>(() => _factory.CreateSelect(query));
    }

    [TestMethod]
    public void Deve_Rejeitar_Expressao_Que_Nao_Seja_Propriedade_Direta() {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new CrudQuery<Usuario>().Where(
                x => x.Nome.Length,
                CrudFilterOperator.GreaterThan,
                3));
    }

    [TestMethod]
    public void Deve_Criar_Select_Por_Chave() {
        var command = _factory.CreateSelectByKey<Usuario>([42]);

        Assert.AreEqual(CrudOperationType.Select, command.OperationType);
        Assert.HasCount(1, command.Filters);
        Assert.AreEqual("ID", command.Filters.Single().ColumnName);
        Assert.AreEqual(42, command.Filters.Single().Value);
    }

    [TestMethod]
    public void Deve_Validar_Quantidade_De_Chaves_Do_Select() {
        Assert.ThrowsExactly<ArgumentException>(() =>
            _factory.CreateSelectByKey<Usuario>(Array.Empty<object?>()));
    }

    [TestMethod]
    public void Deve_Criar_Select_Por_Chave_Nomeada() {
        var command = _factory.CreateSelectByKey<Usuario>(
            new Dictionary<string, object?> { [nameof(Usuario.Id)] = 42 });

        Assert.AreEqual(42, command.Filters.Single().Value);
    }

    [TestMethod]
    public void Deve_Criar_Count_Somente_Com_Filtros() {
        var query = new CrudQuery<Usuario>()
            .Where(x => x.Nome, CrudFilterOperator.Like, "Mar%")
            .OrderBy(x => x.Nome)
            .Page(1, 10);

        var command = _factory.CreateCount(query);

        Assert.AreEqual(CrudOperationType.Count, command.OperationType);
        Assert.HasCount(1, command.Filters);
        Assert.IsEmpty(command.Fields);
        Assert.IsEmpty(command.Sorts);
        Assert.IsNull(command.Pagination);
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
