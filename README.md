# fcs-bff

Backend for Frontend da plataforma **Conexao Solidaria**. O servico atua como fachada HTTP para os clientes `fcs-web`, `fcs-mobile` e `fcs-ui`, encaminhando chamadas para `fcs-identity`, `fcs-campaign` e `fcs-donations`.

> Servico que compoe o MVP da Conexao Solidaria junto a `fcs-identity`, `fcs-campaign`, `fcs-donations`, `fcs-donation-worker`, `fcs-audit-logs`, `fcs-web`, `fcs-mobile`, `fcs-ui`, `fcs-infra` e `fcs-pipelines`.

---

## Responsabilidades

- Centralizar a base publica consumida pelos frontends.
- Encaminhar autenticacao, cadastro, perfil, campanhas, transparencia e doacoes para as APIs de dominio.
- Preservar `Authorization`, `Cookie`, `Set-Cookie`, `X-Correlation-ID`, status codes e response bodies dos downstreams.
- Adaptar rotas de conveniencia do frontend, como `PATCH /api/v1/campaigns/{id}/complete` e `PATCH /api/v1/campaigns/{id}/cancel`.
- Expor `/health` e `/metrics` do proprio BFF.

O BFF **nao** e dono de dominio, banco, Kafka, sessao ou RBAC. Validacao de JWT e autorizacao continuam nas APIs de dominio.

Documentacao completa da arquitetura: [group10-tc-01/fcs-fase05-docs](https://github.com/group10-tc-01/fcs-fase05-docs).

Referencias diretas:

- [Visao geral da arquitetura](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/architecture/overview.md)
- [Endpoints consolidados](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/architecture/endpoints.md)
- [Repositorios e infraestrutura](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/architecture/repositories-and-infra.md)
- [ADR 0028 - APIM como borda publica](https://github.com/group10-tc-01/fcs-fase05-docs/blob/main/adr/0028-use-azure-api-management-as-public-edge.md)

---

## Estrutura do projeto

```text
src/
  Fcs.Bff.WebApi/                 # Controllers proxy, settings, observabilidade e pipeline HTTP
tests/
  Fcs.Bff.UnitTests/              # Testes de regras auxiliares do proxy
  Fcs.Bff.IntegratedTests/        # Testes HTTP do BFF com TestServer
```

---

## Endpoints

Base path publico versionado: `/api/v1`.

| Metodo | Rota | Downstream |
| --- | --- | --- |
| POST | `/api/v1/auth/register/donor` | `fcs-identity` |
| POST | `/api/v1/auth/login` | `fcs-identity` |
| POST | `/api/v1/auth/refresh` | `fcs-identity` |
| POST | `/api/v1/auth/logout` | `fcs-identity` |
| GET | `/api/v1/me` | `fcs-identity` |
| GET | `/api/v1/transparency/campaigns` | `fcs-campaign` |
| GET | `/api/v1/campaigns` | `fcs-campaign` |
| GET | `/api/v1/campaigns/active` | `fcs-campaign` transparency |
| GET | `/api/v1/campaigns/{id}` | `fcs-campaign` |
| POST | `/api/v1/campaigns` | `fcs-campaign` |
| PUT | `/api/v1/campaigns/{id}` | `fcs-campaign` |
| PATCH | `/api/v1/campaigns/{id}/status` | `fcs-campaign` |
| PATCH | `/api/v1/campaigns/{id}/complete` | `fcs-campaign` status `Completed` |
| PATCH | `/api/v1/campaigns/{id}/cancel` | `fcs-campaign` status `Canceled` |
| POST | `/api/v1/donations` | `fcs-donations` |
| GET | `/api/v1/donations` | `fcs-donations` |
| GET | `/api/v1/donations/admin` | `fcs-donations` |
| GET | `/api/v1/donations/{id}` | `fcs-donations` |
| GET | `/health` | BFF |
| GET | `/metrics` | BFF |

Rotas internas dos downstreams, `/health` e `/metrics` dos downstreams nao sao publicadas pelo BFF.

---

## Configuracao

`src/Fcs.Bff.WebApi/appsettings.json`:

```json
{
  "DownstreamServices": {
    "IdentityBaseUrl": "http://localhost:5001",
    "CampaignBaseUrl": "http://localhost:5002",
    "DonationsBaseUrl": "http://localhost:5003"
  }
}
```

Em Docker, o perfil `appsettings.Docker.json` usa os nomes dos servicos na rede:

```json
{
  "DownstreamServices": {
    "IdentityBaseUrl": "http://fcs-identity:8080",
    "CampaignBaseUrl": "http://fcs-campaign:8080",
    "DonationsBaseUrl": "http://fcs-donations:8080"
  }
}
```

---

## Execucao local

```powershell
dotnet run --project src/Fcs.Bff.WebApi --urls http://localhost:5004
```

Healthcheck:

```powershell
curl http://localhost:5004/health
```

Com Docker:

```powershell
docker compose up --build
```

---

## Testes

```powershell
dotnet test Fcs.Bff.slnx --configuration Release
```

Cobertura atual:

- Testes unitarios de headers do proxy.
- Testes integrados de `/health`.
- Testes integrados de encaminhamento de headers/query string.
- Testes integrados de adaptacao `complete -> status Completed`.

---

## CI/CD

Os workflows reutilizam `fcs-pipelines`:

- `.github/workflows/branch-name-check.yml`
- `.github/workflows/dotnet-service-ci.yml`
- `.github/workflows/dotnet-service-cd.yml`

O CD esta configurado com `deploy_to_aks: false` por padrao. O deploy AKS deve ser habilitado apenas quando existirem manifests e variaveis de ambiente do cluster.
