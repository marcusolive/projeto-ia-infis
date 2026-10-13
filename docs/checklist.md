# Checklist do projeto CorporativoAI

Documento vivo do que já está implementado e do que ainda falta.  
**Atualizar automaticamente** ao concluir ou iniciar tarefas nas próximas interações.

Legenda: `[x]` feito · `[ ]` pendente · `[-]` parcial / placeholder

Última revisão: 2026-10-13 (etapa 9 — RabbitMQ, Mailhog, Outbox publisher, Worker/Inbox)

---

## Infraestrutura

- [x] Solution `CorporativoAI.sln` com `Api.Atendimento` e `Api.Conhecimento`
- [x] Projeto `Worker.Atendimento` (Worker Service)
- [x] Docker Compose: PostgreSQL + pgvector (`5433`) e PgAdmin (`5051`)
- [x] Docker Compose: MongoDB (`27018`) e mongo-express (`5052`)
- [x] Docker Compose: SQL Server (`1434`) e Adminer (`5053`)
- [x] Docker Compose: RabbitMQ (`5673` / UI `15673`)
- [x] Docker Compose: Mailhog (`1026` SMTP / UI `8026`)
- [x] Script init Postgres (`conhecimento_chunks` + extensão `vector`)
- [x] Script init SQL Server (`consumo_tokens` + `OutboxMessage` + `InboxMessage`)
- [ ] Documentação Docker/README operacional completa (além do compose)

## API Conhecimento (MCP + RAG)

- [x] ASP.NET Core + Swagger + health
- [x] MCP Server em `/mcp`
- [x] Tool `BuscarConhecimento` (embedding + busca vetorial)
- [x] Chunking de documentos
- [x] Embeddings OpenAI (`text-embedding-3-small`)
- [x] Persistência de chunks/embeddings no Postgres (pgvector)
- [x] `POST /api/v1/ingest` (PDF multipart: usuário, código, validade, arquivo)
- [x] Extração de texto PDF (PdfPig) + limite 2 MB
- [x] Substituição de chunks ao reingerir o mesmo `codigoDocumento`
- [-] Base em memória / carregamento markdown legado (ainda existe código auxiliar; busca principal usa Postgres)
- [ ] Ingestão de formatos além de PDF (ex.: MD, DOCX)
- [ ] Autenticação/autorização no endpoint de ingestão

## API Atendimento (orquestração)

- [x] ASP.NET Core + Swagger + health
- [x] Cliente MCP para a Conhecimento
- [x] Endpoints de diagnóstico MCP (`/conhecimento/tools`, `/conhecimento/buscar`)
- [x] `POST /atendimentos` (e `/perguntas` de compatibilidade)
- [x] Orquestrador: histórico → MCP/RAG → OpenAI → persistências
- [x] Prompt especializado tributário/contábil + Structured Outputs
- [x] Resposta JSON (`resposta`, `categoria`, `fontes`, `encaminharSuporteHumano`, `resumoEncaminhamento`, `modelo`, tokens, custo, `fallback`)
- [x] Memória conversacional no MongoDB (últimas 3 por usuário)
- [x] Registro de tokens e custo estimado no SQL Server
- [x] Integração Gemini (`gemini-3.5-flash-lite`) via `Google.GenAI`
- [x] Fallback Gemini quando OpenAI falhar (`fallback: true` + custos Gemini / Express Mode `UseVertexAi`)
- [x] Flag `encaminharSuporteHumano` na saída estruturada
- [x] Prompt detecta pedido explícito de humano OU insatisfação com o atendimento
- [x] Função Semantic Kernel `encaminhar_atendimento_humano` grava `OutboxMessage` no SQL Server
- [x] `OutboxPublisherBackgroundService` (a cada 1 min, TOP 5) → fila RabbitMQ `atendimentos`
- [x] Atualização de `OutboxMessage` (`Transmitido`, `DataHoraTransmissao`, `NomeFila`)
- [ ] Autenticação/autorização nos endpoints de atendimento

## Worker.Atendimento

- [x] Consumo da fila RabbitMQ `atendimentos`
- [x] Gravação de `InboxMessage` (Id, IdMensagemFila, DataHora, NomeFila, Usuario, Descricao, StatusEnvioEmail)
- [x] Envio de e-mail HTML via Mailhog (remetente/destinatário/assunto conforme spec)
- [ ] DLQ / retry avançado para mensagens com falha permanente

## Requisitos não funcionais

- [x] ASP.NET Core
- [x] async/await e Dependency Injection
- [x] CancellationToken nos fluxos principais
- [x] Logging básico
- [-] Tratamento de erros (presente de forma básica; padronizar Problem Details / políticas)
- [-] API Keys fora do código (suporte a `OPENAI_API_KEY`, `GEMINI_API_KEY` / `GOOGLE_API_KEY` / User Secrets; chaves ainda podem estar em `appsettings` local)
- [ ] Remover definitivamente secrets de `appsettings*.json` versionáveis
- [-] Testes automatizados (apenas similaridade de cosseno em `Api.Conhecimento.Tests`)
- [ ] Testes de integração: ingest → Postgres → MCP → atendimento
- [ ] Testes de Atendimento (orquestrador, custo, histórico, encaminhamento/outbox)
- [ ] Testes do Worker (fila → inbox → e-mail)

## Documentação (`/docs`)

- [x] `requirements.md`
- [x] `atendimento.md`
- [x] `ingest.md`
- [x] `outbox-worker.md`
- [x] `checklist.md` (este arquivo)
- [ ] `architecture.md` (vazio — preencher)
- [ ] `specifications.md` (vazio — preencher)
- [x] `tasks.md` (etapas definidas; etapa 9 concluída no código)

---

## Como atualizar este arquivo

1. Ao implementar algo listado como `[ ]` ou `[-]`, marcar `[x]` (ou ajustar para `[-]` se parcial).
2. Ao descobrir escopo novo, **adicionar** itens — não apagar histórico sem motivo.
3. Atualizar a data em **Última revisão**.
4. Manter alinhado com o código real (preferir verificação no repo, não só a memória do chat).
