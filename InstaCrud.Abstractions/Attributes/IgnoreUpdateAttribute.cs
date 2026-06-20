namespace InstaCrud.Decorators;

[AttributeUsage(AttributeTargets.Property)]
public sealed class IgnoreUpdateAttribute : Attribute {
}