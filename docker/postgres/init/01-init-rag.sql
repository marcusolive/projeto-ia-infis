-- Extensão para busca vetorial
CREATE EXTENSION IF NOT EXISTS vector;

-- Chunks de conhecimento para RAG
CREATE TABLE IF NOT EXISTS conhecimento_chunks (
    id              BIGSERIAL PRIMARY KEY,
    agente_id       VARCHAR(100) NOT NULL,
    documento_codigo VARCHAR(100) NOT NULL,
    documento_nome  VARCHAR(255) NOT NULL,
    incluido_em     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    valido_ate      TIMESTAMPTZ NULL,
    usuario         VARCHAR(150) NOT NULL,
    conteudo        TEXT NOT NULL,
    embedding       vector(1536) NOT NULL,
    indice_chunk    INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT uq_conhecimento_chunks_agente_doc_indice
        UNIQUE (agente_id, documento_codigo, indice_chunk)
);

CREATE INDEX IF NOT EXISTS ix_conhecimento_chunks_agente_documento
    ON conhecimento_chunks (agente_id, documento_codigo);

CREATE INDEX IF NOT EXISTS ix_conhecimento_chunks_valido_ate
    ON conhecimento_chunks (valido_ate);

-- Índice de similaridade de cosseno (pgvector)
CREATE INDEX IF NOT EXISTS ix_conhecimento_chunks_embedding_hnsw
    ON conhecimento_chunks
    USING hnsw (embedding vector_cosine_ops);

COMMENT ON TABLE conhecimento_chunks IS 'Chunks de documentos corporativos com embeddings para RAG.';
COMMENT ON COLUMN conhecimento_chunks.agente_id IS 'Identificador do agente que utiliza o conhecimento.';
COMMENT ON COLUMN conhecimento_chunks.documento_codigo IS 'Código único estável do documento de origem.';
COMMENT ON COLUMN conhecimento_chunks.documento_nome IS 'Nome amigável/arquivo do documento.';
COMMENT ON COLUMN conhecimento_chunks.incluido_em IS 'Data e hora de inclusão do chunk.';
COMMENT ON COLUMN conhecimento_chunks.valido_ate IS 'Data de validade do chunk; NULL = sem expiração.';
COMMENT ON COLUMN conhecimento_chunks.usuario IS 'Usuário responsável pela ingestão.';
COMMENT ON COLUMN conhecimento_chunks.conteudo IS 'Texto do chunk.';
COMMENT ON COLUMN conhecimento_chunks.embedding IS 'Vetor de embedding (text-embedding-3-small, 1536 dims).';
COMMENT ON COLUMN conhecimento_chunks.indice_chunk IS 'Posição do chunk dentro do documento.';
