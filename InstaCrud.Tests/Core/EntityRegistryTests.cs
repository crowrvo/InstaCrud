using InstaCrud.Core;
using InstaCrud.Handler;
using InstaCrud.Exceptions;

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

        Assert.ThrowsExactly<EntityNotRegisteredException>(
            () => registry.Get(typeof(Usuario)));
    }

    [TestMethod]
    public void Deve_Rejeitar_Rotas_Duplicadas() {
        Assert.ThrowsExactly<CrudConfigurationException>(() => new EntityRegistry([
            CreateDefinition("usuarios"),
            CreateDefinition("USUARIOS")
        ]));
    }

    [TestMethod]
    public void Deve_Rejeitar_Tipo_Duplicado() {
        Assert.ThrowsExactly<CrudConfigurationException>(() => new EntityRegistry([
            CreateDefinition("usuarios"),
            CreateDefinition("clientes")
        ]));
    }

    [TestMethod]
    public void Deve_Rejeitar_Rota_Vazia() {
        Assert.ThrowsExactly<CrudConfigurationException>(() => new EntityRegistry([
            CreateDefinition(" ")
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
