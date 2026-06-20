namespace InstantCrud.Abstractions.CrudCommand;

public enum CrudFilterOperator {
    Equal,
    NotEqual,

    GreaterThan,
    GreaterThanOrEqual,

    LessThan,
    LessThanOrEqual,

    Like,
    In,

    IsNull,
    IsNotNull
}