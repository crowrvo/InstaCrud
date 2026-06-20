namespace InstaCrud.Enums;

[Flags]
public enum CrudPropertyFlags {
    None = 0,

    Key = 1,

    DatabaseGenerated = 2,

    Ignore = 4,

    IgnoreInsert = 8,

    IgnoreUpdate = 16
}