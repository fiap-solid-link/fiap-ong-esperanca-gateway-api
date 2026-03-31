# Esperanca Gateway API

API Gateway do projeto **ONG Esperanca**, responsavel por ser o ponto unico de entrada para todos os clientes externos. Construido com **ASP.NET Core 10** e **YARP (Yet Another Reverse Proxy)**.

## Visao Geral

O Gateway abstrai a topologia interna dos microsservicos e centraliza funcionalidades transversais:

```
Cliente (Browser/Mobile)
        |
        v
  +-----------+
  |  Gateway  |  <- CORS, Rate Limiting, Logging
  +-----------+
    |         |
    v         v
Identity   Campanhas
  API        API
```

### Responsabilidades

| Funcionalidade | Descricao |
|----------------|-----------|
| **Roteamento** | Encaminha requisicoes para os microsservicos corretos com base no prefixo da URL |
| **CORS** | Configura headers de cross-origin centralizadamente |
| **Rate Limiting** | Protege os servicos com limite de requisicoes por janela fixa (por IP) |
| **Health Checks** | Agrega o status de saude dos servicos downstream em `/health` |
| **Logging** | Logging estruturado com Serilog (console + Application Insights) |
| **Auth Pass-Through** | Repassa o header `Authorization` sem validar — cada servico valida seu proprio JWT |

### O que o Gateway NAO faz

- **NAO valida JWT** — a autorizacao e responsabilidade de cada microsservico
- **NAO faz load balancing** — o Kubernetes cuida disso
- **NAO faz cache** — sem cache de resposta no gateway
- **NAO documenta APIs** — cada servico documenta seus proprios endpoints

## Rotas Configuradas

| Rota | Servico Destino | Descricao |
|------|-----------------|-----------|
| `/api/identity/**` | `identity-api` | Registro, autenticacao, emissao de JWT |
| `/api/campanhas/**` | `campanhas-api` | CRUD de campanhas (GestorONG) |
| `/api/doacoes/**` | `campanhas-api` | Submissao de doacoes (Doador) |
| `/api/transparencia/**` | `campanhas-api` | Relatorios publicos de transparencia |
| `/health` | Gateway (agregado) | Status de saude do gateway e servicos downstream |

## Pre-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (opcional, para build containerizado)

## Como Executar

### Desenvolvimento local

```bash
cd src/Esperanca.Gateway
dotnet run
```

O gateway sobe em `http://localhost:5000`. No ambiente `Development`, as rotas apontam para:
- Identity API: `http://localhost:5017`

Swagger UI disponivel em: `http://localhost:5000/swagger/index.html`

### Com Docker

```bash
docker build -t esperanca-gateway .
docker run -p 8080:8080 esperanca-gateway
```

## Configuracao

Toda a configuracao fica em `appsettings.json`. O arquivo `appsettings.Development.json` sobrescreve valores para o ambiente local.

### CORS

```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"]
  }
}
```

### Rate Limiting

Limite de requisicoes por IP usando janela fixa. Retorna HTTP `429 Too Many Requests` quando excedido.

```json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60
  }
}
```

### Application Insights

Para habilitar o envio de telemetria, adicione a connection string:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=..."
  }
}
```

## Como Adicionar um Novo Microsservico

Para rotear um novo microsservico atraves do gateway, sao necessarios **3 passos** no `appsettings.json`:

### Passo 1 — Criar o Cluster

Adicione uma entrada em `ReverseProxy.Clusters` com o endereco do servico:

```json
{
  "ReverseProxy": {
    "Clusters": {
      "novo-servico-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://fiap-ong-esperanca-novo-servico-api"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Timeout": "00:00:10",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          }
        }
      }
    }
  }
}
```

> **Dica:** O `Address` deve ser o DNS interno do servico no Kubernetes. Para desenvolvimento local, sobrescreva em `appsettings.Development.json` com `http://localhost:<porta>`.

### Passo 2 — Criar a(s) Rota(s)

Adicione uma entrada em `ReverseProxy.Routes` apontando para o cluster criado:

```json
{
  "ReverseProxy": {
    "Routes": {
      "novo-servico-route": {
        "ClusterId": "novo-servico-cluster",
        "RateLimiterPolicy": "fixed",
        "Match": {
          "Path": "/api/novo-servico/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/novo-servico/{**catch-all}" }
        ]
      }
    }
  }
}
```

**Campos importantes:**

| Campo | Descricao |
|-------|-----------|
| `ClusterId` | Nome do cluster definido no Passo 1 |
| `RateLimiterPolicy` | Politica de rate limiting (`"fixed"` para usar a padrao) |
| `Match.Path` | Padrao de URL que ativa esta rota. `{**catch-all}` captura todo o restante do path |
| `Transforms.PathPattern` | Como o path e reescrito antes de encaminhar ao servico. Mantenha igual ao `Match.Path` para preservar a URL original |

> **Multiplas rotas para o mesmo cluster:** E possivel ter varias rotas apontando para o mesmo cluster (como `/api/campanhas/**`, `/api/doacoes/**` e `/api/transparencia/**` que vao para `campanhas-cluster`).

### Passo 3 — Adicionar o Health Check (opcional)

Se quiser que o endpoint `/health` do gateway monitore o novo servico, adicione em `Program.cs`:

```csharp
builder.Services.AddHealthChecks()
    // ... health checks existentes ...
    .AddUrlGroup(
        new Uri(builder.Configuration["HealthChecks:NovoServico:Uri"]
            ?? "http://fiap-ong-esperanca-novo-servico-api/health"),
        name: "novo-servico-api",
        timeout: TimeSpan.FromSeconds(10));
```

E adicione a URI configuravel no `appsettings.json`:

```json
{
  "HealthChecks": {
    "NovoServico": {
      "Uri": "http://fiap-ong-esperanca-novo-servico-api/health"
    }
  }
}
```

### Exemplo Completo

Supondo que voce quer adicionar um servico de **notificacoes** em `/api/notificacoes/**`:

**appsettings.json:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "notificacoes-route": {
        "ClusterId": "notificacoes-cluster",
        "RateLimiterPolicy": "fixed",
        "Match": {
          "Path": "/api/notificacoes/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/notificacoes/{**catch-all}" }
        ]
      }
    },
    "Clusters": {
      "notificacoes-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://fiap-ong-esperanca-notificacoes-api"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Timeout": "00:00:10",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          }
        }
      }
    }
  }
}
```

**appsettings.Development.json:**
```json
{
  "ReverseProxy": {
    "Clusters": {
      "notificacoes-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5003"
          }
        }
      }
    }
  }
}
```

## Fluxo de Autenticacao

```
1. Cliente envia POST /api/identity/login com credenciais
2. Gateway encaminha para Identity API
3. Identity API retorna JWT (access token + refresh token)
4. Cliente armazena o JWT

5. Cliente envia requisicao com header Authorization: Bearer <token>
6. Gateway repassa a requisicao com o header intacto para o servico destino
7. Servico destino valida o JWT com a chave simetrica compartilhada (HMAC-SHA256)
8. Servico extrai roles dos claims e aplica [Authorize(Roles = "...")]
```

> O JWT contem os claims: `sub` (userId), `email`, `roles` (array). A chave simetrica e compartilhada entre os servicos via Kubernetes Secret.

## Estrutura do Projeto

```
fiap-ong-esperanca-gateway-api/
├── .gitignore
├── Dockerfile                            # Build multi-stage para container
├── Esperanca.Gateway.sln
├── README.md
└── src/
    └── Esperanca.Gateway/
        ├── Extensions/
        │   ├── CorsExtensions.cs         # Configuracao de CORS
        │   ├── HealthCheckExtensions.cs   # Health checks agregados + response writer
        │   ├── LoggingExtensions.cs       # Serilog + Application Insights
        │   ├── RateLimitingExtensions.cs  # Rate limiting por IP (fixed window)
        │   └── ReverseProxyExtensions.cs  # YARP setup
        ├── Esperanca.Gateway.csproj
        ├── Program.cs                    # Startup limpo — apenas chamadas de extensao
        ├── appsettings.json              # Configuracao de producao (rotas, clusters, policies)
        └── appsettings.Development.json  # Overrides para desenvolvimento local
```

## Tecnologias

| Componente | Tecnologia |
|------------|-----------|
| Framework | ASP.NET Core 10 |
| Reverse Proxy | YARP 2.3.0 |
| Rate Limiting | Microsoft.AspNetCore.RateLimiting (nativo .NET) |
| Logging | Serilog |
| Observabilidade | Application Insights |
| Health Checks | AspNetCore.HealthChecks.Uris |
