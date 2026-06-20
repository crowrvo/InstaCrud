using InstaCrud.Decorators;
using System.Reflection;

[TestClass]
public class MetadataReflectionTests {
    [Crud("anexos")]
    [Table("TAB_CONSERV_ARQ")]
    private class EntidadeTeste {
        [Key]
        [Column("NUM_ARQ")]
        public int NumArq { get; set; }

        [Column("OBSERVACOES")]
        public string? Observacoes { get; set; }
    }

    [TestMethod]
    public void Deve_Ler_Todos_Os_Decoradores() {
        var tipo = typeof(EntidadeTeste);

        var crud = tipo.GetCustomAttribute<CrudAttribute>();
        var table = tipo.GetCustomAttribute<TableAttribute>();

        Assert.IsNotNull(crud);
        Assert.IsNotNull(table);

        Assert.AreEqual("anexos", crud.RouteName);
        Assert.AreEqual("TAB_CONSERV_ARQ", table.Name);

        var chave = tipo.GetProperties()
            .Single(x => x.IsDefined(typeof(KeyAttribute)));

        Assert.AreEqual("NumArq", chave.Name);
    }
}