namespace Api.Conhecimento.Models;

public sealed class ChunkParaPersistencia
{
    public required string AgenteId { get; init; }
    public required string DocumentoCodigo { get; init; }
    public required string DocumentoNome { get; init; }
    public required DateTimeOffset ValidoAte { get; init; }
    public required string Usuario { get; init; }
    public required string Conteudo { get; init; }
    public required float[] Embedding { get; init; }
    public required int IndiceChunk { get; init; }
}
