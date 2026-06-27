namespace InstaCrud.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnAttribute : Attribute {
    public string Name { get; }

    public ColumnAttribute(string name) {
        Name = name;
    }
}