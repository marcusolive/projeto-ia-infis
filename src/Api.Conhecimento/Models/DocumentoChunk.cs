namespace Api.Conhecimento.Models;

public sealed class DocumentoChunk
{
    public required string Id { get; init; }
    public required string Fonte { get; init; }
    public required string Conteudo { get; init; }
    public required float[] Embedding { get; init; }
    public int Indice { get; init; }
}
