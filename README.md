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
| `InstaCrud.Dapper` | Geração e execução de comandos por Dapper | Em desenvolvimento |
| `InstaCrud.AspNetCore` | Integração com DI e endpoints HTTP | Planejado |
| `InstaCrud.EFCore` | Provider para Entity Framework Core | Planejado |
| `InstaCrud.Sql` | Dialetos SQL Server e Oracle | Em desenvolvimento |
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

### Extensões e providers

O contrato `ICrudProvider<TResult>` não pressupõe SQL nem Dapper. O `Core` transforma a entidade em um comando neutro e cada extensão decide como processá-lo:

- `InstaCrud.Dapper` traduz o comando para SQL Server e opcionalmente o executa em uma `IDbConnection`;
- `InstaCrud.EFCore` poderá aplicar a operação a um `DbContext` sem reutilizar o executor Dapper;
- `InstaCrud.AspNetCore` poderá localizar um engine registrado e publicar endpoints, sem conhecer detalhes do banco;
- `InstaCrud.Kria` poderá fornecer seu próprio resultado e ciclo de execução quando sua API estiver disponível.

Assim, atributos, registro, regras de campos e criação de comandos são compartilhados. Conexão, transação, materialização e sintaxe específica permanecem isoladas no provider.

SQL Server é o dialeto padrão. Para gerar ou executar SQL Oracle, informe o dialeto explicitamente:

```csharp
var oracleCommands = DapperCrud.Create<Usuario>(OracleDialect.Instance);

var oracleExecutor = DapperCrud.CreateExecutor<Usuario>(
    oracleConnection,
    OracleDialect.Instance);
```

O dialeto Oracle cobre identificadores, parâmetros, paginação com `OFFSET/FETCH` e recuperação de chaves geradas com `RETURNING INTO`. Os parâmetros de saída são tipados e seus valores são atribuídos de volta à entidade após a execução.

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

## Uso atual

O primeiro fluxo funcional gera SQL Server e parâmetros diretamente da entidade:

```csharp
var crud = DapperCrud.Create<Usuario>();

var usuario = new Usuario {
    Nome = "Maria",
    CriadoEm = DateTime.UtcNow
};

SqlCommandDefinition insert = crud.Insert(usuario);

// insert.Sql:
// INSERT INTO [USUARIO] ([NOME], [CriadoEm])
// OUTPUT INSERTED.[ID] VALUES (@p0, @p1);

// O objeto e sua chave também geram update e delete:
SqlCommandDefinition update = crud.Update(usuario);
SqlCommandDefinition delete = crud.Delete(usuario);

// Patch recebe os nomes das propriedades permitidas:
SqlCommandDefinition patch = crud.Patch(usuario, nameof(Usuario.Nome));

// Também é possível registrar várias entidades:
var applicationCrud = DapperCrud.Create(
    typeof(Usuario),
    typeof(Produto));
```

O resultado contém o SQL e um dicionário de parâmetros que também podem ser consumidos diretamente pela aplicação.

Para executar diretamente, a aplicação fornece e continua responsável pelo ciclo de vida da conexão:

```csharp
await using var connection = new SqlConnection(connectionString);
var executor = DapperCrud.CreateExecutor<Usuario>(connection);

await executor.InsertAsync(usuario, cancellationToken: cancellationToken);
await executor.UpdateAsync(usuario, cancellationToken: cancellationToken);

IReadOnlyList<Usuario> usuarios =
    await executor.SelectAsync<Usuario>(cancellationToken: cancellationToken);
```

Quando a chave possui `[DatabaseGenerated]`, o insert usa `OUTPUT INSERTED` no SQL Server ou `RETURNING INTO` no Oracle e atribui o valor retornado à entidade. Transações e timeout podem ser informados em cada operação. O executor nunca descarta a conexão recebida; seu ciclo de vida continua pertencendo à aplicação.

Filtros, projeção, ordenação e paginação podem ser definidos por propriedades do modelo:

```csharp
var query = new CrudQuery<Usuario>()
    .Select(x => x.Id, x => x.Nome)
    .Where(x => x.Ativo, CrudFilterOperator.Equal, true)
    .OrderBy(x => x.Nome)
    .Page(1, 50);

IReadOnlyList<Usuario> usuariosAtivos =
    await executor.SelectAsync(query, cancellationToken: cancellationToken);

Usuario? usuario = await executor.FindAsync<Usuario>(id);
long totalAtivos = await executor.CountAsync(query);
PagedResult<Usuario> pagina = await executor.PageAsync(query);
```

As expressões aceitam somente acesso direto a propriedades. O nome informado em `[Column]` é resolvido pelo registro antes de chegar ao provider, impedindo que identificadores SQL arbitrários sejam introduzidos pela consulta.

Chaves compostas podem ser consultadas por um dicionário com os nomes das propriedades `[Key]`, evitando dependência da ordem de declaração:

```csharp
var chave = new Dictionary<string, object?> {
    [nameof(ItemPedido.PedidoId)] = pedidoId,
    [nameof(ItemPedido.ItemId)] = itemId
};

ItemPedido? item = await executor.FindAsync<ItemPedido>(chave);
```

`PageAsync` executa a consulta dos itens e a contagem sequencialmente. Quando ambos precisarem enxergar exatamente o mesmo estado do banco, forneça uma transação ao método.

## Estado do MVP

SQL Server e Oracle são os bancos prioritários. Outros bancos deverão receber dialetos ou providers próprios depois que esses dois contratos estiverem estáveis.

### Progresso

```text
MVP funcional  [████████████████░░░░] 78%
```

Estimativa atualizada em 15 de setembro de 2026. O percentual considera apenas o MVP de persistência; ASP.NET Core, EF Core, Kria e providers futuros não bloqueiam esse marco.

| Área | Peso | Entregue | Situação |
| --- | ---: | ---: | --- |
| Arquitetura e contratos | 10% | 10% | Concluído |
| Metadados e registro | 15% | 12% | Faltam validações de borda |
| Comandos CRUD e consultas | 20% | 20% | Concluído |
| Dialeto SQL Server | 15% | 12% | Falta validação em banco real |
| Dialeto Oracle | 15% | 10% | Falta validar tipos e `RETURNING INTO` reais |
| Execução Dapper | 10% | 8% | Falta endurecer ciclo de conexão e erros |
| Testes automatizados | 10% | 3% | 48 testes unitários; integração pendente |
| Documentação e exemplo | 5% | 3% | README pronto; aplicação sample pendente |
| **Total** | **100%** | **78%** | **MVP avançado, ainda não publicável** |

### O que falta para concluir o MVP

1. Criar testes de integração executados contra SQL Server e Oracle.
2. Validar insert, identidade, select, update, patch, delete, paginação e rollback nos dois bancos.
3. Confirmar `RETURNING INTO` com o driver Oracle e os tipos CLR suportados.
4. Endurecer validações de metadados: nomes duplicados, propriedades ilegíveis, chaves inválidas e rotas conflitantes.
5. Definir exceções públicas e comportamento quando nenhuma ou múltiplas linhas forem afetadas.
6. Criar uma aplicação mínima reproduzível com entidades, schema e ciclo CRUD completo.

### Critério de conclusão

O MVP estará concluído quando uma aplicação de exemplo conseguir executar automaticamente o ciclo abaixo em SQL Server e Oracle, com os mesmos modelos e consultas:

```text
registrar entidade
    -> inserir e recuperar chave
    -> buscar e paginar
    -> atualizar e aplicar patch
    -> excluir
    -> confirmar commit/rollback
```

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

- [x] estabilizar os contratos públicos iniciais;
- [x] concluir o registro e a leitura de entidades;
- [x] gerar comandos SQL Server a partir de entidades;
- [x] executar comandos assíncronos por uma conexão Dapper;
- [x] isolar as sintaxes SQL Server e Oracle;
- [x] oferecer consultas tipadas com filtro, projeção, ordenação e paginação;
- [x] oferecer busca por chave, contagem e resultado paginado;
- [x] suportar `RETURNING INTO` e chaves geradas no Oracle;
- [ ] validar o ciclo CRUD em uma instância SQL Server;
- [ ] validar o ciclo CRUD em uma instância Oracle;
- [ ] criar uma aplicação de exemplo reproduzível;
- [ ] endurecer validações e exceções públicas;
- [ ] adicionar CI para build e testes;
- [ ] projetar a integração ASP.NET Core;
- [ ] avaliar providers adicionais;
- [ ] definir empacotamento, versionamento e publicação no NuGet.

## Licença

Ainda não definida.
