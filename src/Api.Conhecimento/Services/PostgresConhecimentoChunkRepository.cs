using Api.Conhecimento.Models;
using Npgsql;
using Pgvector;

namespace Api.Conhecimento.Services;

public sealed class PostgresConhecimentoChunkRepository : IConhecimentoChunkRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PostgresConhecimentoChunkRepository> _logger;

    public PostgresConhecimentoChunkRepository(
        NpgsqlDataSource dataSource,
        ILogger<PostgresConhecimentoChunkRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task SubstituirDocumentoAsync(
        string agenteId,
        string documentoCodigo,
        IReadOnlyList<ChunkParaPersistencia> chunks,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agenteId);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentoCodigo);
        ArgumentNullException.ThrowIfNull(chunks);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var deleteCmd = new NpgsqlCommand(
                """
                DELETE FROM conhecimento_chunks
                WHERE agente_id = @agenteId
                  AND documento_codigo = @documentoCodigo;
                """,
                connection,
                transaction))
            {
                deleteCmd.Parameters.AddWithValue("agenteId", agenteId);
                deleteCmd.Parameters.AddWithValue("documentoCodigo", documentoCodigo);
                var removidos = await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation(
                    "Removidos {Quantidade} chunks anteriores do documento {Documento}.",
                    removidos,
                    documentoCodigo);
            }

            const string insertSql =
                """
                INSERT INTO conhecimento_chunks
                    (agente_id, documento_codigo, documento_nome, valido_ate, usuario, conteudo, embedding, indice_chunk)
                VALUES
                    (@agenteId, @documentoCodigo, @documentoNome, @validoAte, @usuario, @conteudo, @embedding, @indiceChunk);
                """;

            foreach (var chunk in chunks)
            {
                await using var insertCmd = new NpgsqlCommand(insertSql, connection, transaction);
                insertCmd.Parameters.AddWithValue("agenteId", chunk.AgenteId);
                insertCmd.Parameters.AddWithValue("documentoCodigo", chunk.DocumentoCodigo);
                insertCmd.Parameters.AddWithValue("documentoNome", chunk.DocumentoNome);
                insertCmd.Parameters.AddWithValue("validoAte", chunk.ValidoAte.UtcDateTime);
                insertCmd.Parameters.AddWithValue("usuario", chunk.Usuario);
                insertCmd.Parameters.AddWithValue("conteudo", chunk.Conteudo);
                insertCmd.Parameters.AddWithValue("embedding", new Vector(chunk.Embedding));
                insertCmd.Parameters.AddWithValue("indiceChunk", chunk.IndiceChunk);
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Persistidos {Quantidade} chunks do documento {Documento} para o agente {Agente}.",
                chunks.Count,
                documentoCodigo,
                agenteId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<ChunkSimilaridade>> BuscarPorSimilaridadeAsync(
        string agenteId,
        float[] embeddingConsulta,
        int topK,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agenteId);
        ArgumentNullException.ThrowIfNull(embeddingConsulta);

        if (topK <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(topK), "topK deve ser maior que zero.");
        }

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            """
            SELECT
                documento_nome,
                conteudo,
                indice_chunk,
                1 - (embedding <=> @embedding) AS similaridade
            FROM conhecimento_chunks
            WHERE agente_id = @agenteId
              AND (valido_ate IS NULL OR valido_ate >= NOW())
            ORDER BY embedding <=> @embedding
            LIMIT @topK;
            """,
            connection);

        cmd.Parameters.AddWithValue("agenteId", agenteId);
        cmd.Parameters.AddWithValue("embedding", new Vector(embeddingConsulta));
        cmd.Parameters.AddWithValue("topK", topK);

        var resultados = new List<ChunkSimilaridade>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            resultados.Add(new ChunkSimilaridade
            {
                Fonte = reader.GetString(0),
                Conteudo = reader.GetString(1),
                Indice = reader.GetInt32(2),
                Similaridade = reader.GetDouble(3)
            });
        }

        _logger.LogInformation(
            "Busca RAG no Postgres retornou {Quantidade} chunks para o agente {Agente}.",
            resultados.Count,
            agenteId);

        return resultados;
    }
}
