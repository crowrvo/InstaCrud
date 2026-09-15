using InstaCrud.Core;
using InstaCrud.Handler;

namespace InstaCrud.Tests.Core;

[TestClass]
public sealed class EntityRegistryTests {
    private sealed class Usuario;

    [TestMethod]
    public void Deve_Encontrar_Entidade_Por_Tipo_E_Rota() {
        var definition = CreateDefinition("usuarios");
        var registry = new EntityRegistry([definition]);

        Assert.AreSame(definition, registry.Get(typeof(Usuario)));
        Assert.AreSame(definition, registry.Get("USUARIOS"));
    }

    [TestMethod]
    public void Deve_Rejeitar_Tipo_Nao_Registrado() {
        var registry = new EntityRegistry([]);

        Assert.ThrowsExactly<KeyNotFoundException>(
            () => registry.Get(typeof(Usuario)));
    }

    [TestMethod]
    public void Deve_Rejeitar_Rotas_Duplicadas() {
        Assert.ThrowsExactly<ArgumentException>(() => new EntityRegistry([
            CreateDefinition("usuarios"),
            CreateDefinition("USUARIOS")
        ]));
    }

    private static CrudEntityDefinition CreateDefinition(string routeName) => new() {
        EntityType = typeof(Usuario),
        RouteName = routeName,
        TableName = "USUARIO",
        Properties = [],
        KeyProperties = []
    };
}
