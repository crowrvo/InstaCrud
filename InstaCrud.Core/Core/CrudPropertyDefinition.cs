namespace InstaCrud.Core;

using InstaCrud.Enums;
using System.Reflection;

public sealed class CrudPropertyDefinition {
    public required PropertyInfo PropertyInfo { get; init; }

    public required string PropertyName { get; init; }

    public required string ColumnName { get; init; }

    public required Type PropertyType { get; init; }

    public required CrudPropertyFlags Flags { get; init; }

    public Func<object, object?>? Getter { get; init; }

    public Action<object, object?>? Setter { get; init; }

    public bool IsKey =>
        Flags.HasFlag(CrudPropertyFlags.Key);

    public bool IsDatabaseGenerated =>
        Flags.HasFlag(CrudPropertyFlags.DatabaseGenerated);

    public bool Ignore =>
        Flags.HasFlag(CrudPropertyFlags.Ignore);

    public bool IgnoreInsert =>
        Flags.HasFlag(CrudPropertyFlags.IgnoreInsert);

    public bool IgnoreUpdate =>
        Flags.HasFlag(CrudPropertyFlags.IgnoreUpdate);

    public bool IgnorePatch =>
        Flags.HasFlag(CrudPropertyFlags.IgnorePatch);
}