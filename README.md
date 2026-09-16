# InstantCrud

InstantCrud é uma biblioteca .NET para criar operações CRUD completas a partir de uma classe que representa uma tabela do banco de dados.

A proposta é direta: depois da configuração da conexão e da infraestrutura, o desenvolvedor cria a classe que representa a tabela e não escreve código CRUD. O InstantCrud descobre os metadados e gera persistência, consultas, filtros, endpoints HTTP e documentação OpenAPI.

> O projeto está em desenvolvimento inicial e ainda não possui uma versão estável publicada.
>
> O nome público definido para o produto é **InstantCrud**. Os projetos e namespaces ainda usam `InstaCrud` e serão padronizados antes da primeira versão pública.

## Objetivo

Eliminar o código repetitivo necessário para criar e expor CRUDs convencionais sem esconder as decisões importantes de persistência.

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
- gerar consultas filtradas, ordenadas e paginadas;
- publicar controllers ou endpoints ASP.NET Core automaticamente;
- incluir operações, contratos e schemas no OpenAPI, prontos para consumo.

O objetivo final é que a classe seja todo o código específico necessário para uma tabela. Insert, select, query filter, update, PUT, PATCH e delete devem ser derivados automaticamente do modelo e de seus atributos.

## O que o InstantCrud não é

InstantCrud **não é um ORM**. Ele não pretende oferecer change tracking, unit of work, lazy loading, migrations, resolução automática de relacionamentos ou um provedor LINQ completo.

Seu núcleo é um **query builder orientado por metadados**: transforma classes e operações conhecidas em comandos seguros e os entrega a um mediador como Dapper, EF Core ou Kria. A camada ASP.NET Core utiliza os mesmos metadados para gerar a superfície HTTP, sem transformar o núcleo em framework web ou ORM.

Essa separação é intencional:

- o modelo descreve tabela, colunas, chaves e restrições de escrita;
- o query builder cria comandos de banco parametrizados;
- o provider executa os comandos com a tecnologia escolhida;
- a extensão ASP.NET Core gera o CRUD HTTP e o OpenAPI.

## Princípios

- **Modelo como fonte de verdade:** nomes de tabela e coluna usados pelo runtime são obtidos dos metadados da entidade.
- **Segurança por padrão:** valores são parametrizados e identificadores externos nunca são enviados diretamente ao SQL.
- **Núcleo independente:** descoberta e comandos CRUD não dependem de Dapper, EF Core ou frameworks web.
- **Providers substituíveis:** cada tecnologia de persistência pode implementar os contratos públicos sem contaminar o núcleo.
- **Adoção incremental:** uma aplicação pode utilizar somente os pacotes necessários.
- **Zero boilerplate por tabela:** nenhuma classe de repository, service ou controller deve ser obrigatória para o CRUD convencional.

## Arquitetura

| Projeto | Responsabilidade | Estado |
| --- | --- | --- |
| `InstaCrud.Abstractions` | Atributos, comandos e contratos públicos | Em desenvolvimento |
| `InstaCrud.Core` | Descoberta, validação e registro de metadados | Em desenvolvimento |
| `InstaCrud.Dapper` | Geração e execução de comandos por Dapper | Em desenvolvimento |
| `InstaCrud.AspNetCore` | Integração com DI e endpoints HTTP | Planejado |
| `InstaCrud.EFCore` | Provider para Entity Framework Core | Planejado |
| `InstaCrud.Sql` | Dialetos SQL Server, Oracle e SQLite | Em desenvolvimento |
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
    -> controllers ou endpoints gerados
    -> OpenAPI pronto para uso
```

### Extensões e providers

O contrato `ICrudProvider<TResult>` não pressupõe SQL nem Dapper. O `Core` transforma a entidade em um comando neutro e cada extensão decide como processá-lo:

- `InstaCrud.Dapper` traduz o comando para o dialeto SQL selecionado e opcionalmente o executa em uma `IDbConnection`;
- `InstaCrud.EFCore` poderá aplicar a operação a um `DbContext` sem reutilizar o executor Dapper;
- `InstaCrud.AspNetCore` poderá localizar um engine registrado e publicar endpoints, sem conhecer detalhes do banco;
- `InstaCrud.Kria` poderá fornecer seu próprio resultado e ciclo de execução quando sua API estiver disponível.

Assim, atributos, registro, regras de campos e criação de comandos são compartilhados. Conexão, transação, materialização e sintaxe específica permanecem isoladas no provider.

### Experiência final desejada

Depois de configurar o provider e a conexão uma única vez, cada tabela deve exigir somente seu modelo:

```csharp
[Crud("usuarios")]
[Table("USUARIO")]
public sealed class Usuario
{
    [Key]
    [DatabaseGenerated]
    public int Id { get; set; }

    public required string Nome { get; set; }
}
```

A visão para a inicialização da aplicação é deliberadamente pequena:

```csharp
builder.Services.AddInstantCrud(options =>
    options.UseSqlServer(connectionString));

app.MapInstantCrud();
```

> `AddInstantCrud` e `MapInstantCrud` representam a API planejada e ainda não estão implementados.

Com isso, o runtime deverá gerar automaticamente:

- `POST /usuarios` para insert;
- `GET /usuarios` para select, filtros, ordenação e paginação;
- `GET /usuarios/{id}` para busca por chave;
- `PUT /usuarios/{id}` para atualização completa;
- `PATCH /usuarios/{id}` para atualização parcial;
- `DELETE /usuarios/{id}` para exclusão;
- schemas, parâmetros, respostas e operações correspondentes no OpenAPI.

SQL Server é o dialeto padrão. Para gerar ou executar SQL Oracle, informe o dialeto explicitamente:

```csharp
var oracleCommands = DapperCrud.Create<Usuario>(OracleDialect.Instance);

var oracleExecutor = DapperCrud.CreateExecutor<Usuario>(
    oracleConnection,
    OracleDialect.Instance);
```

O dialeto Oracle cobre identificadores, parâmetros, paginação com `OFFSET/FETCH` e recuperação de chaves geradas com `RETURNING INTO`. Os parâmetros de saída são tipados e seus valores são atribuídos de volta à entidade após a execução.

SQLite também está disponível como dialeto de apoio ao desenvolvimento. Ele mantém o exemplo e os testes de integração autocontidos, sem alterar a prioridade de produção em SQL Server e Oracle.

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

Quando a chave possui `[DatabaseGenerated]`, o insert usa `OUTPUT INSERTED` no SQL Server, `RETURNING INTO` no Oracle ou `RETURNING` no SQLite e atribui o valor retornado à entidade. Transações e timeout podem ser informados em cada operação. O executor nunca descarta a conexão recebida; seu ciclo de vida continua pertencendo à aplicação.

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

## Exemplo executável

O projeto `samples/InstantCrud.Sample` demonstra o ciclo CRUD completo com SQLite em memória. Ele cria o schema ao iniciar e não exige servidor, arquivo de banco ou configuração externa:

```shell
dotnet run --project samples/InstantCrud.Sample/InstantCrud.Sample.csproj
```

O exemplo insere entidades e recupera suas chaves geradas, faz busca e consulta paginada, executa update e patch, exclui um registro e confirma a contagem final. O banco existe somente durante a execução do processo.

## Estado do MVP

SQL Server e Oracle são os bancos prioritários. SQLite é mantido como dialeto leve para o exemplo e a suíte de integração; outros bancos deverão receber dialetos ou providers próprios depois que os contratos prioritários estiverem estáveis.

### Progresso

```text
MVP funcional  [█████████████████░░░] 86%
Produto final  [████████████░░░░░░░░] 60%
```

Estimativa atualizada em 16 de setembro de 2026. O primeiro percentual considera o MVP de persistência. O segundo inclui a geração ASP.NET Core/OpenAPI e a preparação para distribuição. EF Core, Kria e providers futuros não bloqueiam a primeira versão completa baseada em Dapper.

| Área | Peso | Entregue | Situação |
| --- | ---: | ---: | --- |
| Arquitetura e contratos | 10% | 10% | Concluído |
| Metadados e registro | 15% | 15% | Validações estruturais concluídas |
| Comandos CRUD e consultas | 20% | 20% | Concluído |
| Dialeto SQL Server | 15% | 12% | Falta validação em banco real |
| Dialeto Oracle | 15% | 10% | Falta validar tipos e `RETURNING INTO` reais |
| Execução Dapper | 10% | 8% | Falta endurecer ciclo de conexão e erros |
| Testes automatizados | 10% | 6% | 60 testes; ciclo CRUD e rollback validados em SQLite |
| Documentação e exemplo | 5% | 5% | Exemplo SQLite autocontido e reproduzível |
| **Total** | **100%** | **86%** | **MVP avançado, ainda não publicável** |

### O que falta para concluir o MVP

1. Criar testes de integração executados contra SQL Server e Oracle.
2. Validar insert, identidade, select, update, patch, delete, paginação e rollback nos dois bancos.
3. Confirmar `RETURNING INTO` com o driver Oracle e os tipos CLR suportados.
4. Definir o comportamento quando nenhuma ou múltiplas linhas forem afetadas.

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

A geração ASP.NET Core/OpenAPI será o marco seguinte ao MVP de persistência e faz parte do objetivo final do produto. EF Core e Kria continuarão como providers posteriores.

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
- [x] validar o ciclo CRUD e rollback em SQLite em memória;
- [ ] validar o ciclo CRUD em uma instância SQL Server;
- [ ] validar o ciclo CRUD em uma instância Oracle;
- [x] criar uma aplicação de exemplo reproduzível;
- [x] endurecer validações de metadados e conflitos no registro;
- [x] criar exceções públicas para configuração e entidades não registradas;
- [ ] definir resultados e exceções para operações de persistência;
- [ ] adicionar CI para build e testes;
- [ ] padronizar projetos e namespaces com o nome público `InstantCrud`;
- [ ] implementar registro por DI com `AddInstantCrud`;
- [ ] gerar controllers ou endpoints CRUD com `MapInstantCrud`;
- [ ] gerar contratos, parâmetros e respostas no OpenAPI;
- [ ] avaliar providers adicionais;
- [ ] definir empacotamento, versionamento e publicação no NuGet.

## Licença

Ainda não definida.
