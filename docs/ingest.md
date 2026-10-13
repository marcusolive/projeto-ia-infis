# Ingestão de PDFs

A `Api.Conhecimento` expõe `POST /api/v1/ingest` para cadastrar conhecimento a partir de arquivos **PDF**. O fluxo valida o arquivo, extrai o texto, divide em chunks, gera embeddings (OpenAI) e persiste no PostgreSQL com **pgvector** (`bd-rag-infis.conhecimento_chunks`).

## Processamento

1. Recebe `multipart/form-data` com:
   - `usuario` — responsável pela ingestão
   - `codigoDocumento` — código estável do documento
   - `validade` — data/hora de validade dos chunks
   - `arquivo` — PDF
2. Valida metadados e o arquivo:
   - Extensão/content-type PDF e magic bytes `%PDF`
   - Tamanho máximo **2 MB** (`Rag:TamanhoMaximoPdfBytes`)
3. Extrai texto com **UglyToad.PdfPig**.
4. Divide o texto em chunks (`ChunkSize` / `ChunkOverlap`).
5. Gera embeddings em lote (`text-embedding-3-small`, 1536 dimensões).
6. Remove chunks anteriores do mesmo `(agente_id, documento_codigo)` e grava os novos em transação.
7. Responde **201 Created** com:
   ```json
   { "mensagem": "Conhecimento cadastrado com sucesso" }
   ```

Campos persistidos em `conhecimento_chunks`:

| Campo | Descrição |
|-------|-----------|
| `id` | BIGSERIAL |
| `agente_id` | Agente padrão (`Rag:AgenteIdPadrao`) |
| `documento_codigo` | Código informado |
| `documento_nome` | Nome do arquivo PDF |
| `incluido_em` | UTC (default `now()`) |
| `valido_ate` | Validade informada |
| `usuario` | Usuário da ingestão |
| `conteudo` | Texto do chunk |
| `embedding` | `vector(1536)` |
| `indice_chunk` | Posição no documento |

A busca MCP (`BuscarConhecimento`) usa esses embeddings com distância de cosseno (`<=>`) e retorna os top K chunks ainda válidos.

## Configuração

`Api.Conhecimento` (`appsettings`):

| Chave | Função |
|-------|--------|
| `ConnectionStrings:Rag` | Postgres `localhost:5433` / `bd-rag-infis` |
| `OpenAI:ApiKey` | Chave para embeddings |
| `OpenAI:EmbeddingModel` | Ex.: `text-embedding-3-small` |
| `Rag:ChunkSize` | Tamanho do chunk (padrão 500) |
| `Rag:ChunkOverlap` | Overlap (padrão 50) |
| `Rag:TopK` | Chunks retornados na busca (padrão 3) |
| `Rag:AgenteIdPadrao` | Ex.: `agente-corporativo` |
| `Rag:TamanhoMaximoPdfBytes` | 2097152 (2 MB) |

Infra Docker:

| Serviço | Porta | Uso |
|---------|-------|-----|
| PostgreSQL + pgvector | `5433` | Base RAG |
| PgAdmin | `5051` | GUI SQL |

Script de schema: `docker/postgres/init/01-init-rag.sql`.

## Execução e testes

Subir infra e API:

```bash
docker compose up -d postgres pgadmin
dotnet run --project src/Api.Conhecimento --launch-profile http
```

Exemplo de chamada (multipart):

```http
POST http://localhost:5081/api/v1/ingest
Content-Type: multipart/form-data

usuario=infis
codigoDocumento=DOC-001
validade=2027-12-31T23:59:59Z
arquivo=@documento.pdf
```

Swagger: http://localhost:5081/swagger

Validação sugerida:

1. Container Postgres healthy.
2. Extensão `vector` e tabela `conhecimento_chunks` existentes.
3. Upload de PDF ≤ 2 MB retorna **201** e mensagem de sucesso.
4. PDF inválido / > 2 MB retorna **400**.
5. Consultar no PgAdmin: novos registros com `documento_codigo` e embeddings.
6. Reenviar o mesmo `codigoDocumento` substitui os chunks anteriores.
7. `tools/call` MCP `BuscarConhecimento` com consulta relacionada retorna trechos do PDF ingerido.

Testes automatizados atuais relacionados a RAG:

- `tests/Api.Conhecimento.Tests` — similaridade de cosseno (não cobre o endpoint de ingest ponta a ponta).
