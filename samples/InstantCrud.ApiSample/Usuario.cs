using InstaCrud.Abstractions.Attributes;

namespace InstantCrud.ApiSample;

[Crud("usuarios")]
[Table("USUARIO")]
public sealed class Usuario {
    [Key]
    [DatabaseGenerated]
    [Column("ID")]
    public long Id { get; set; }

    [Column("NOME")]
    public required string Nome { get; set; }

    [Column("EMAIL")]
    public required string Email { get; set; }

    [Column("ATIVO")]
    public bool Ativo { get; set; }

    [IgnoreUpdate]
    [Column("CRIADO_EM")]
    public DateTime CriadoEm { get; set; }
}
