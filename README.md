# CaseFraudSys

Sistema de gestão de limites PIX para o Banco KRT — desafio técnico BTG Pactual (FraudSys).

A API permite que o analista de fraudes cadastre, consulte, altere e remova limites PIX por conta, e que transações PIX sejam avaliadas com débito atômico no limite disponível.

## Stack

- .NET 8 (ASP.NET Core Web API)
- Amazon DynamoDB (local para desenvolvimento)
- Arquitetura em camadas: Domain, Application, Infrastructure, Presentation (MVC)
- Testes: xUnit + Moq (unitário), WebApplicationFactory (integração), NBomber (carga)

## Decisões de design

| Decisão | Escolha |
|---------|---------|
| Identificador da conta | Par `agencia` + `conta` (único no sistema) |
| CPF | Dado cadastral obrigatório; validação de 11 dígitos |
| Valores monetários | `decimal` em reais (ex.: `1500.50`) |
| Concorrência PIX | `ConditionExpression` no DynamoDB (`PixLimit >= :amount`) |
| Idempotência PIX | `transactionId` obrigatório; registro `PK = TX#{transactionId}` |
| Chave DynamoDB | `PK = AGENCY#{agencia}#ACCOUNT#{conta}` (contas) ou `TX#{transactionId}` (idempotência) |

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Docker (para DynamoDB Local)

## Como executar

### 1. Subir o DynamoDB Local

**Com Docker (recomendado):**

```powershell
docker compose up -d
```

**Sem Docker:**

```powershell
.\scripts\start-dynamodb-local.ps1
```

O DynamoDB ficará disponível em `http://localhost:8000`.

### 2. Executar a API

```powershell
cd src\CaseFraudSys.Api
dotnet run
```

- API: `http://localhost:5067`
- Swagger (Development): `http://localhost:5067/swagger` — documentação em português com tags, erros e schemas tipados

### Logs no console

Todas as rotas registram logs via `ILogger` no terminal ao executar `dotnet run`:

- **Entrada** — método HTTP, rota e parâmetros relevantes (agência, conta, valores)
- **Sucesso** — resumo do resultado (limite consultado, transação aprovada/negada, etc.)
- **Erros de negócio** (400, 404, 409) — logados como `Warning` pelo middleware

Exemplo:

```
info: CaseFraudSys.Api.Presentation.Controllers.AccountLimitsController[0]
      GET /api/account-limits/0001/12345 — limite consultado: 5000.00
warn: CaseFraudSys.Api.Infrastructure.Middleware.ErrorHandlingMiddleware[0]
      GET /api/account-limits/0001/99999 — 404: Conta não encontrada.
```

### Dados pré-carregados (Development)

Em ambiente **Development**, a API popula automaticamente a tabela com contas de teste ao iniciar (configurado em `appsettings.Development.json`). Isso permite testar GET, PUT, DELETE e PIX **sem precisar cadastrar via POST primeiro**.

| Agência | Conta | CPF | Limite PIX |
|---------|-------|-----|------------|
| `0001` | `12345` | `12345678901` | `5000.00` |
| `0002` | `67890` | `98765432100` | `1000.00` |

O seed é **idempotente**: se a conta já existir, não é sobrescrita. Como o DynamoDB Local roda em memória (`-inMemory`), os dados são perdidos ao reiniciar o container — o seed repovoa automaticamente na próxima subida da API.

## Testes

### Objetivo

Garantir que os requisitos do desafio BTG (2.1–2.5) funcionem corretamente, com foco em automação reproduzível e rastreabilidade requisito × caso de teste.

### Pirâmide de testes

| Camada | Projeto | Comando | Dependências |
|--------|---------|---------|--------------|
| Unitário | `tests/CaseFraudSys.Api.Tests` | `dotnet test tests/CaseFraudSys.Api.Tests --filter "Category=Unit"` | Nenhuma |
| Integração API | `tests/CaseFraudSys.Api.IntegrationTests` | `dotnet test tests/CaseFraudSys.Api.IntegrationTests --filter "Category=Integration"` | DynamoDB Local :8000 |
| Carga (NBomber) | `tests/CaseFraudSys.LoadTests` | `dotnet run --project tests/CaseFraudSys.LoadTests` | API + DynamoDB |

### Como executar localmente

```powershell
docker compose up -d
dotnet test tests/CaseFraudSys.Api.Tests --filter "Category=Unit"
dotnet test tests/CaseFraudSys.Api.IntegrationTests --filter "Category=Integration"
```

Para rodar todos os projetos de teste xUnit de uma vez:

```powershell
dotnet test
```

Testes de integração exigem DynamoDB Local em `http://localhost:8000`.

### Matriz requisito × casos

| Req | Cenário | Tipo | Automatizado |
|-----|---------|------|--------------|
| 2.1 | Cadastro válido | Happy | Integration |
| 2.1 | CPF inválido | Negative | Unit, Integration |
| 2.1 | Conta duplicada (409) | Negative | Unit, Integration |
| 2.2 | Consultar limite | Happy | Integration |
| 2.2 | Conta inexistente (404) | Negative | Unit, Integration |
| 2.3 | Alterar limite | Happy | Integration |
| 2.4 | Remover registro | Happy | Integration |
| 2.5 | PIX aprovado desconta limite | Happy | Unit, Integration |
| 2.5 | PIX negado não altera limite | Negative | Unit, Integration |
| 2.5 | Idempotência (mesmo transactionId) | Negative/Happy | Unit, Integration |

### Cobertura de código

**Meta:** >= **80%** de cobertura de linhas em `CaseFraudSys.Api` (unitários + integração mesclados), verificada **localmente**.

**Excluídos do cálculo:** `Program.cs`, `SwaggerExtensions.cs`, `DynamoDbDataSeeder.cs` (bootstrap e seed).

```powershell
docker compose up -d
./scripts/check-coverage.ps1
```

Comando manual (PowerShell):

```powershell
# 1. Instale a ferramenta (uma vez)
dotnet tool install -g dotnet-reportgenerator-globaltool

# 2. Rode os testes com cobertura
dotnet test tests/CaseFraudSys.Api.Tests --filter "Category=Unit" --collect:"XPlat Code Coverage" --settings tests/coverlet.runsettings --results-directory TestResults
dotnet test tests/CaseFraudSys.Api.IntegrationTests --filter "Category=Integration" --collect:"XPlat Code Coverage" --settings tests/coverlet.runsettings --results-directory TestResults

# 3. Gere o relatório (use reportgenerator, NÃO dotnet reportgenerator)
$reports = (Get-ChildItem TestResults -Recurse -Filter coverage.cobertura.xml | ForEach-Object FullName) -join ';'
reportgenerator "-reports:$reports" "-targetdir:coveragereport" "-reporttypes:TextSummary" "-assemblyfilters:+CaseFraudSys.Api"
```

### CI

GitHub Actions em [`.github/workflows/tests.yml`](.github/workflows/tests.yml):

- Job `unit-tests`: testes unitários
- Job `integration-tests`: DynamoDB Local + testes de integração

Cobertura e gate de 80% são verificados **localmente** com `check-coverage.ps1` (o CI não bloqueia por cobertura).

### Testes de carga (NBomber)

Os testes de carga são **separados** dos testes unitários e exigem a API em execução.

**Terminal 1 e 2:** DynamoDB + API (mesmos passos acima).

**Terminal 3:**

```powershell
dotnet run --project tests\CaseFraudSys.LoadTests
```

| Cenário | Rota | Parâmetros padrão | Objetivo |
|---------|------|-------------------|----------|
| `health_load` | `GET /api/health` | 50 req/s por 30s | Baseline de disponibilidade |
| `get_account_limit` | `GET /api/account-limits/0001/12345` | 100 req/s por 60s | Latência de consulta |
| `pix_concurrent` | `POST /api/pix/transactions` | 50 req/s por 4s (200 total) | Débito atômico concorrente |

Parâmetros configuráveis em [`tests/CaseFraudSys.LoadTests/appsettings.json`](tests/CaseFraudSys.LoadTests/appsettings.json). Override da URL:

```powershell
$env:LOAD_TEST_BASE_URL = "http://localhost:5067"
dotnet run --project tests\CaseFraudSys.LoadTests
```

**Relatório:** ao final, o NBomber gera relatórios txt/csv/md em `./reports/` (pasta gitignored). Métricas principais:

- **RPS** — requisições por segundo
- **Latency (p95/p99)** — latência percentil
- **Fail %** — taxa de falha (deve ser 0% com API saudável)

O cenário `pix_concurrent` cria/reseta a conta `0001/99999` com limite `100000.00` e valida no console se o limite final bate com o esperado após 200 débitos de `10.00`.

### Fora de escopo

- Testes de UI (sem frontend)
- Autenticação/autorização (não exigida no PDF)
- DynamoDB AWS real

## Endpoints

Todos os endpoints retornam o envelope padrão:

```json
{
  "success": true,
  "message": "...",
  "data": { },
  "traceId": null
}
```

### Health

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/health` | Verifica se a API está ativa |

### Gestão de limites (requisitos 2.1–2.4)

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/account-limits` | Cadastrar limite PIX |
| GET | `/api/account-limits/{agency}/{account}` | Consultar limite |
| PUT | `/api/account-limits/{agency}/{account}` | Alterar limite PIX |
| DELETE | `/api/account-limits/{agency}/{account}` | Remover registro |

### Transações PIX (requisito 2.5)

Toda transação exige `transactionId` (idempotência). Reenvios com o mesmo ID retornam `isDuplicate: true` sem novo débito.

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/pix/transactions` | Avaliar e processar transação PIX |

## Exemplos

### Cadastrar limite

```http
POST http://localhost:5067/api/account-limits
Content-Type: application/json

{
  "document": "12345678901",
  "agency": "0001",
  "account": "12345",
  "pixLimit": 5000.00
}
```

### Consultar limite

```http
GET http://localhost:5067/api/account-limits/0001/12345
```

### Alterar limite

```http
PUT http://localhost:5067/api/account-limits/0001/12345
Content-Type: application/json

{
  "pixLimit": 3000.00
}
```

### Transação PIX aprovada

```http
POST http://localhost:5067/api/pix/transactions
Content-Type: application/json

{
  "transactionId": "tx-001-aprovada",
  "agency": "0001",
  "account": "12345",
  "amount": 1500.00
}
```

Resposta (aprovada):

```json
{
  "success": true,
  "message": "Transação PIX aprovada.",
  "data": {
    "transactionId": "tx-001-aprovada",
    "approved": true,
    "remainingLimit": 3500.00,
    "currentLimit": 5000.00,
    "isDuplicate": false
  }
}
```

### Reenvio idempotente (mesmo transactionId)

Reenviar o mesmo payload retorna a resposta em cache sem novo débito:

```json
{
  "success": true,
  "message": "Transação PIX aprovada.",
  "data": {
    "transactionId": "tx-001-aprovada",
    "approved": true,
    "remainingLimit": 3500.00,
    "currentLimit": 5000.00,
    "isDuplicate": true
  }
}
```

### Transação PIX negada

Quando o valor excede o limite, a transação é negada e o limite **não é alterado**.

## Estrutura do projeto

```
src/CaseFraudSys.Api/
├── Domain/           # Entidades, interfaces de repositório
├── Application/      # DTOs, serviços, validadores
├── Infrastructure/   # DynamoDB, middleware
└── Presentation/     # Controllers MVC

tests/CaseFraudSys.Api.Tests/
├── Application/              # Services, validadores
├── Presentation/             # Controllers
└── Infrastructure/           # Repositórios, middleware, mappers

tests/CaseFraudSys.Api.IntegrationTests/
├── Fixtures/                 # WebApplicationFactory + ApiTestHelper
└── *EndpointTests.cs         # CRUD, PIX, idempotência

tests/CaseFraudSys.LoadTests/
├── Scenarios/                # NBomber (health, GET, PIX concorrente)
└── appsettings.json
```
