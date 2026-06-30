# Documentacao da implementacao do fcs-bff

Data: 2026-06-29

## Objetivo

Estruturar o repositorio `fcs-bff` como Backend for Frontend da Conexao Solidaria e preparar os clientes para consumirem uma unica base HTTP publica.

O BFF foi implementado como proxy/adaptador fino. Ele nao gerencia sessao, banco, Kafka nem regras de dominio. JWT/RBAC continuam sob responsabilidade das APIs de dominio.

## O que foi implementado

### Repositorio `fcs-bff`

- Solution `Fcs.Bff.slnx`.
- Projeto `src/Fcs.Bff.WebApi`.
- Testes `tests/Fcs.Bff.UnitTests`.
- Testes `tests/Fcs.Bff.IntegratedTests`.
- Dockerfile e `docker-compose.yml`.
- Workflows GitHub Actions usando `fcs-pipelines`.
- README no padrao dos demais repositorios.

### Web API

- Controllers proxy para:
  - `AuthController`
  - `MeController`
  - `CampaignsController`
  - `TransparencyController`
  - `DonationsController`
- `ProxyService` baseado em `IHttpClientFactory`.
- Preservacao de headers de ponta a ponta:
  - `Authorization`
  - `Cookie`
  - `Set-Cookie`
  - `X-Correlation-ID`
  - demais headers nao hop-by-hop
- Encaminhamento de query string, request body, status code e response body.
- Adaptacao:
  - `PATCH /api/v1/campaigns/{id}/complete` vira `PATCH /api/v1/campaigns/{id}/status` com `status=Completed`.
  - `PATCH /api/v1/campaigns/{id}/cancel` vira `PATCH /api/v1/campaigns/{id}/status` com `status=Canceled`.

### Observabilidade e operacao

- `/health`.
- `/metrics`.
- Swagger.
- CORS configuravel.
- Correlation ID.
- Serilog.
- OpenTelemetry.

### Clientes preparados localmente

Tambem foram ajustados localmente:

- `fcs-web`
  - `API_CONFIG.bffBaseUrl`.
  - Auth e `me` via BFF.
  - Transparencia via BFF.
  - Transparencia lendo `ApiResponse<PagedResponse<T>>`.
- `fcs-mobile`
  - `API_CONFIG.bffBaseUrl`.
  - Auth e `me` via BFF.
  - Transparencia via BFF.
  - Transparencia lendo `ApiResponse<PagedResponse<T>>`.
- `fcs-ui`
  - Configs apontando para BFF.
  - `Fcs.UI.Web` lendo configuracao em vez de URLs hardcoded.
  - Refit clients ajustados para `PagedResponse<T>` em campanhas e doacoes.

## Validacoes executadas

```powershell
dotnet test Fcs.Bff.slnx --configuration Release
```

Resultado: passou.

```text
Fcs.Bff.UnitTests: 8 testes passando
Fcs.Bff.IntegratedTests: 3 testes passando
```

Tambem foram validados os clientes alterados:

```powershell
npm.cmd run test:ci
npm.cmd run build
```

Resultados:

- `fcs-web`: testes passaram; build passou com warning antigo de budget.
- `fcs-mobile`: testes passaram; build passou.
- `fcs-ui`: ainda nao compila porque o projeto `src/Fcs.UI.Cache/Fcs.UI.Cache.csproj` esta ausente no clone.

## O que ainda precisa implementar

### No `fcs-bff`

- Adicionar manifests Kubernetes quando o deploy AKS for habilitado.
- Criar testes integrados para todas as rotas proxy, nao apenas amostras.
- Adicionar teste de propagacao de `Set-Cookie` vindo do `fcs-identity`.
- Avaliar rate limiting no APIM ou no BFF, conforme decisao de infra.
- Ajustar CORS final para os dominios reais de producao.

### Nos backends

- Mergear o PR de `fcs-donations` que adiciona:
  - `GET /api/v1/donations/admin`
  - paginacao
  - envelope `ApiResponse<PagedResponse<T>>`
- Corrigir a divergencia de rota interna entre `fcs-donation-worker` e `fcs-campaign`:
  - worker chama `/internal/campaigns/{id}/donation-processed`
  - campaign atual expoe `/api/v1/internal/campaigns/{id}/donation-processed`

### Nos clientes

- Subir separadamente os ajustes locais de:
  - `fcs-web`
  - `fcs-mobile`
  - `fcs-ui`
- Corrigir `fcs-ui` antes de validar/pushar:
  - restaurar ou remover `src/Fcs.UI.Cache/Fcs.UI.Cache.csproj`
- Em `fcs-ui`, revisar se a UI deve continuar usando tres chaves (`Identity`, `Campaign`, `Donations`) apontando para o mesmo BFF ou se deve migrar para uma unica chave `Bff`.

### Na infra

- Incluir `fcs-bff` no `fcs-infra`.
- Publicar `fcs-bff` no APIM como fachada principal dos frontends.
- Garantir que `/internal/*`, `/health` downstream e `/metrics` downstream nao sejam publicados na borda.
- Atualizar docker compose integrado, Kind e AKS manifests.

## O que ainda precisa subir no GitHub

Subir agora:

- Novo repositorio `group10-tc-01/fcs-bff`.

Subir depois, em PRs separados:

- Ajustes do `fcs-web` para `bffBaseUrl`.
- Ajustes do `fcs-mobile` para `bffBaseUrl`.
- Ajustes do `fcs-ui` para BFF e contratos paginados, depois de corrigir `Fcs.UI.Cache`.
- Atualizacoes de `fcs-infra` para incluir `fcs-bff`.
- Atualizacoes de documentacao em `fcs-fase05-docs` citando que o BFF existe e quais rotas publica.

## Decisoes tecnicas tomadas

- BFF como proxy fino, nao como session store.
- Preservar cookies para `fcs-web`.
- Preservar Bearer token para `fcs-mobile` e `fcs-ui`.
- Manter validacao RBAC nas APIs de dominio.
- Nao publicar rotas internas pelo BFF.
- Nome padrao do repositorio: `fcs-bff`.
- Namespace/projeto .NET: `Fcs.Bff`.
