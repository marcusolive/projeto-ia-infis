namespace Api.Conhecimento.Services;

public interface IChunkingService
{
    IReadOnlyList<TextoChunk> Dividir(string fonte, string conteudo);
}

public sealed record TextoChunk(string Fonte, string Conteudo, int Indice);
