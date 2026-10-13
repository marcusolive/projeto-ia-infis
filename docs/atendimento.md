# Atendimento tributário e contábil

A `Api.Atendimento` atua como orquestradora REST de um assistente especializado em **dúvidas tributárias** e **dúvidas contábeis**. Ela obtém contexto via **MCP** na `Api.Conhecimento` (busca RAG por embeddings no PostgreSQL), monta o prompt com histórico conversacional e chama a **OpenAI** com Structured Outputs.

## Fluxo

1. Cliente envia `POST /atendimentos` com:
   - `usuario` — nome do usuário
   - `pergunta` — dúvida tributária/contábil
2. A API recupera as **3 últimas conversas** do usuário no **MongoDB** (`bd-historico-atendimentos` / collection `conversas`).
3. A API chama a tool MCP **`BuscarConhecimento`** na `Api.Conhecimento` (`http://localhost:5081/mcp`).
4. A Conhecimento gera embedding da pergunta (OpenAI), busca os **top 3** chunks no Postgres (`bd-rag-infis.conhecimento_chunks`) por similaridade de cosseno e devolve o contexto.
5. A Atendimento monta o prompt (escopo tributário/contábil + histórico + contexto RAG + pergunta).
6. A OpenAI (`gpt-4o-mini`) responde em JSON estruturado. Se falhar, aciona **fallback Gemini** (`gemini-3.5-flash-lite`) e marca `fallback: true`.
7. Se `encaminharSuporteHumano=true`, o orquestrador invoca a função **Semantic Kernel** `encaminhar_atendimento_humano`, que grava um registro em `dbo.OutboxMessage` (SQL Server).
8. A API calcula o custo estimado dos tokens (preços por provedor), grava a conversa no MongoDB e o consumo no **SQL Server** (`bd-custos-ia.consumo_tokens`).
9. Retorna a resposta estruturada ao cliente.
10. Em paralelo, o `OutboxPublisherBackgroundService` (a cada 1 min) publica pendências na fila RabbitMQ `atendimentos`; o `Worker.Atendimento` consome, grava `InboxMessage` e envia e-mail via Mailhog. Detalhes em [`outbox-worker.md`](outbox-worker.md).

```text
Cliente
  → POST /atendimentos (Api.Atendimento :5080)
    → MongoDB (últimas 3 conversas)
    → MCP Client → Api.Conhecimento /mcp
      → embedding + busca vetorial (Postgres/pgvector)
    → OpenAI Structured Output
      └─ (falha) → Gemini gemini-3.5-flash-lite (fallback)
    → (se encaminharSuporteHumano) Semantic Kernel → OutboxMessage (SQL)
    → MongoDB (grava conversa)
    → SQL Server (grava tokens/custo)
  ← JSON estruturado
```

Endpoints relacionados:

| Método | Rota | Uso |
|--------|------|-----|
| `POST` | `/atendimentos` | Endpoint principal de atendimento |
| `POST` | `/perguntas` | Compatibilidade (mesmo contrato) |
| `GET` | `/conhecimento/tools` | Diagnóstico MCP (listar tools) |
| `POST` | `/conhecimento/buscar` | Diagnóstico MCP (chamar tool) |

## Controle de consumo de tokens

Após cada chamada à OpenAI, a Atendimento:

1. Lê `tokensEntrada` e `tokensSaida` do `Usage` da completion.
2. Calcula custos com `CustoTokenCalculator`, usando preços em USD por 1 milhão de tokens (`CustosTokens` no `appsettings`).
3. Preenche `custoEstimado` na resposta (custo total).
4. Persiste em `dbo.consumo_tokens` no SQL Server.

Campos gravados:

| Campo | Descrição |
|-------|-----------|
| `id` | Identity |
| `agente` | Id do agente (ex.: `assistente-tributario-contabil`) |
| `modelo` | Modelo usado (ex.: `gpt-4o-mini`) |
| `usuario` | Usuário da conversa |
| `data_hora` | UTC |
| `tokens_entrada` / `tokens_saida` | Quantidades |
| `custo_tokens_entrada` / `custo_tokens_saida` / `custo_total` | Valores estimados |
| `id_conversa` | Referência ao `_id` do documento no MongoDB |

Infra:

- SQL Server: `localhost:1434` (container `custos-infis-sqlserver`)
- Adminer: http://localhost:5053 (design `pepa-linha`)
- Banco: `bd-custos-ia` / usuário `sa`

## Memória conversacional

Cada interação é salva no MongoDB:

| Campo | Conteúdo |
|-------|----------|
| `Id` | ObjectId |
| `dataHora` | UTC |
| `usuario` | Nome do usuário |
| `agente` | Agente configurado |
| `pergunta` | Pergunta enviada |
| `respostaEstruturada` | Objeto completo da IA |
| `respostaTexto` | Somente o campo `resposta` |

Na próxima pergunta do mesmo usuário:

- Recupera até **3** conversas anteriores (ordenadas cronologicamente).
- Injeta no prompt na seção `HISTÓRICO DA CONVERSA`.
- O histórico serve para continuidade; **fatos** devem vir do contexto RAG.

Infra:

- MongoDB: `localhost:27018`
- GUI (mongo-express): http://localhost:5052
- Database: `bd-historico-atendimentos` / collection `conversas`

## Resposta

Contrato JSON retornado:

| Campo | Tipo | Origem |
|-------|------|--------|
| `resposta` | string | LLM |
| `categoria` | string | LLM (`tributaria`, `contabil`, `fora_escopo`, `sem_dados`) |
| `fontes` | string[] | LLM (com base no contexto) |
| `encaminharSuporteHumano` | bool | LLM — `true` se o usuário pediu humano ou está insatisfeito |
| `resumoEncaminhamento` | string | LLM — resumo para a outbox (vazio se não encaminhar) |
| `modelo` | string | Configuração / chamada OpenAI ou Gemini |
| `fallback` | bool | `true` quando a resposta veio do Gemini após falha da OpenAI |
| `tokensEntrada` | int | Usage do provedor LLM |
| `tokensSaida` | int | Usage do provedor LLM |
| `custoEstimado` | decimal | Calculado localmente |

### Encaminhamento humano (Outbox)

Critérios no prompt:

1. Pedido explícito de atendimento humano/atendente.
2. Insatisfação clara com o atendimento.

Quando `encaminharSuporteHumano=true`, a função SK grava em `bd-custos-ia.dbo.OutboxMessage`:

| Campo | Default |
|-------|---------|
| `Id` | IDENTITY |
| `DataHoraGravacao` | UTC na gravação |
| `Transmitido` | `false` |
| `DataHoraTransmissao` | `null` |
| `NomeFila` | `null` |
| `Usuario` | usuário da requisição |
| `Descricao` | `resumoEncaminhamento` (ou fallback com a pergunta) |

Exemplo de request:

```http
POST http://localhost:5080/atendimentos
Content-Type: application/json

{
  "usuario": "infis",
  "pergunta": "Qual o tratamento contábil para provisão de IRPJ?"
}
```

## Execução e validação

Pré-requisitos:

```bash
docker compose up -d
```

Subir as APIs (portas típicas):

- Atendimento: http://localhost:5080 (Swagger em `/swagger`)
- Conhecimento: http://localhost:5081 (MCP em `/mcp`)

Checklist rápido de validação:

1. Postgres (RAG), MongoDB e SQL Server healthy.
2. Há documentos ingeridos em `conhecimento_chunks` (via `POST /api/v1/ingest`).
3. `POST /atendimentos` retorna 200 com JSON estruturado.
4. Novo documento aparece na collection `conversas` (Mongo).
5. Nova linha aparece em `consumo_tokens` (SQL Server), com `id_conversa` apontando para o Mongo.
6. Segunda pergunta do mesmo usuário inclui histórico no comportamento da resposta.
7. Pedido explícito de humano (ou insatisfação) retorna `encaminharSuporteHumano: true` e cria linha em `OutboxMessage`.

Configuração relevante (`Api.Atendimento`):

- `McpConhecimento:Endpoint`
- `OpenAI:ChatModel` / `OpenAI:ApiKey` (ou `OPENAI_API_KEY`)
- `Gemini:ChatModel` (`gemini-3.5-flash-lite`) / `Gemini:ApiKey` (ou `GEMINI_API_KEY` / `GOOGLE_API_KEY`)
- `Gemini:UseVertexAi` — `true` para chaves Express Mode (`AQ.*`); `false` para Google AI Studio (`AIza*`); se omitido, detecta pelo prefixo
- `MongoHistorico:*`
- `CustosTokens:*` (inclui preços OpenAI e Gemini)
