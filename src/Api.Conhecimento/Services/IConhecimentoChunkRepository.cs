using Api.Conhecimento.Models;

namespace Api.Conhecimento.Services;

public interface IConhecimentoChunkRepository
{
    Task SubstituirDocumentoAsync(
        string agenteId,
        string documentoCodigo,
        IReadOnlyList<ChunkParaPersistencia> chunks,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChunkSimilaridade>> BuscarPorSimilaridadeAsync(
        string agenteId,
        float[] embeddingConsulta,
        int topK,
        CancellationToken cancellationToken);
}
