using Api.Conhecimento.Models;

namespace Api.Conhecimento.Services;

public interface IBuscaConhecimentoService
{
    Task<IReadOnlyList<ChunkSimilaridade>> BuscarAsync(string consulta, CancellationToken cancellationToken);
}
