namespace InstaCrud.Decorators;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CrudAttribute : Attribute {
    public string? RouteName { get; }

    public CrudAttribute(string? routeName = null) {
        RouteName = routeName;
    }
}