using InstaCrud.Abstractions.Attributes;

namespace InstantCrud.ApiSample;

[Crud("enderecos")]
[Table("ENDERECO")]
public sealed class Endereco {
    [Key]
    [DatabaseGenerated]
    [Column("ID")]
    public long Id { get; set; }

    [Column("RUA")]
    public required string Rua { get; set; }

    [Column("NUMERO")]
    public required int Numero { get; set; }
    
    
    [Column("BAIRRO")]
    public required string Bairro { get; set; }

    [Column("COMPLEMENTO")]
    public string? Complemento { get; set; }

    [Column("ATIVO")]
    public bool Ativo { get; set; }

    [IgnoreUpdate]
    [Column("CRIADO_EM")]
    public DateTime CriadoEm { get; set; }
}
