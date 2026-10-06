using Api.Conhecimento.Models;

namespace Api.Conhecimento.Services;

public interface IVectorStore
{
    void Substituir(IReadOnlyList<DocumentoChunk> chunks);

    IReadOnlyList<DocumentoChunk> Listar();

    int Contagem { get; }
}
