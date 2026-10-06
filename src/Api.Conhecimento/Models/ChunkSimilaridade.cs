namespace Api.Conhecimento.Models;

public sealed class ChunkSimilaridade
{
    public required string Fonte { get; init; }
    public required string Conteudo { get; init; }
    public required double Similaridade { get; init; }
    public int Indice { get; init; }
}
