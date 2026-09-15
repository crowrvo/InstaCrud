namespace InstaCrud.Sql;

public sealed record SqlReturningField(
    string ColumnName,
    string TargetName,
    Type ValueType);
