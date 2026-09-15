# InstaCrud

InstaCrud é uma biblioteca .NET para criar operações CRUD a partir de uma classe que representa uma tabela do banco de dados.

A proposta é manter o modelo de uso simples: descreva a entidade e seus mapeamentos; o InstaCrud descobre os metadados e os entrega a um provider responsável por gerar ou executar as operações de persistência.

> O projeto está em desenvolvimento inicial e ainda não possui uma versão estável publicada.

## Objetivo

Reduzir o código repetitivo necessário para expor CRUDs convencionais sem esconder as decisões importantes de persistência.

```csharp
[Crud("usuarios")]
[Table("USUARIO")]
public sealed class Usuario
{
    [Key]
    [DatabaseGenerated]
    [Column("ID")]
    public int Id { get; set; }

    [Column("NOME")]
    public required string Nome { get; set; }

    [IgnoreUpdate]
    public DateTime CriadoEm { get; set; }
}
```

A partir desse modelo, a biblioteca deverá ser capaz de:

- descobrir entidades marcadas com `[Crud]`;
- construir e armazenar seus metadados;
- traduzir operações de insert, select, update, patch e delete;
- delegar a geração ou execução para um provider;
- opcionalmente publicar essas operações por integrações como ASP.NET Core.

## Princípios

- **Modelo como fonte de verdade:** nomes de tabela e coluna usados pelo runtime são obtidos dos metadados da entidade.
- **Segurança por padrão:** valores são parametrizados e identificadores externos nunca são enviados diretamente ao SQL.
- **Núcleo independente:** descoberta e comandos CRUD não dependem de Dapper, EF Core ou frameworks web.
- **Providers substituíveis:** cada tecnologia de persistência pode implementar os contratos públicos sem contaminar o núcleo.
- **Adoção incremental:** uma aplicação pode utilizar somente os pacotes necessários.

## Arquitetura

| Projeto | Responsabilidade | Estado |
| --- | --- | --- |
| `InstaCrud.Abstractions` | Atributos, comandos e contratos públicos | Em desenvolvimento |
| `InstaCrud.Core` | Descoberta, validação e registro de metadados | Em desenvolvimento |
| `InstaCrud.Dapper` | Tradução de comandos para SQL Server parametrizado | Em desenvolvimento |
| `InstaCrud.AspNetCore` | Integração com DI e endpoints HTTP | Planejado |
| `InstaCrud.EFCore` | Provider para Entity Framework Core | Planejado |
| `InstaCrud.Sql` | Componentes SQL compartilhados, se necessários | Planejado |
| `InstaCrud.Kria` | Integração com o mediador Kria | Fora do escopo atual |
| `InstaCrud.Tests` | Testes unitários e de integração | Em desenvolvimento |

O fluxo pretendido é:

```text
classe anotada
    -> scanner e definição de metadados
    -> registro de entidades
    -> comando CRUD validado
    -> provider selecionado
    -> comando parametrizado / execução
```

## Mapeamento

| Atributo | Uso |
| --- | --- |
| `[Crud]` | Habilita a entidade e opcionalmente define sua rota lógica |
| `[Table]` | Define o nome da tabela |
| `[Column]` | Define o nome da coluna |
| `[Key]` | Marca uma propriedade como parte da chave |
| `[DatabaseGenerated]` | Marca um valor gerado pelo banco |
| `[Ignore]` | Exclui a propriedade de todas as operações |
| `[IgnoreInsert]` | Exclui a propriedade de inserts |
| `[IgnoreUpdate]` | Exclui a propriedade de updates e patches |

Sem `[Table]` ou `[Column]`, o nome do tipo ou da propriedade é utilizado.

## Estado do MVP

O SQL Server é o primeiro dialeto do provider Dapper. Outros bancos deverão receber dialetos ou providers próprios depois que o contrato do MVP estiver estável.

O primeiro marco funcional terá:

1. descoberta e registro de entidades;
2. validação consistente dos metadados;
3. comandos para as cinco operações CRUD;
4. geração de SQL parametrizado pelo provider Dapper;
5. testes unitários da geração e testes de integração;
6. uma aplicação mínima demonstrando o uso.

Integrações com ASP.NET Core, EF Core e Kria serão projetadas depois que esse fluxo estiver estável.

## Desenvolvimento

Requisitos atuais:

- .NET SDK 10;
- MSTest 4.

```shell
dotnet restore
dotnet build InstaCrud.slnx
dotnet test InstaCrud.slnx
```

Mudanças devem manter a solução compilável e incluir testes para comportamentos novos ou corrigidos. Os commits seguem a convenção Conventional Commits, como `feat:`, `fix:`, `test:`, `docs:` e `refactor:`.

## Roadmap

- [ ] estabilizar os contratos públicos;
- [ ] concluir o registro e a validação de entidades;
- [ ] concluir o provider Dapper;
- [ ] criar testes de integração e aplicação de exemplo;
- [ ] projetar a integração ASP.NET Core;
- [ ] avaliar providers adicionais;
- [ ] definir empacotamento, versionamento e publicação no NuGet.

## Licença

Ainda não definida.
