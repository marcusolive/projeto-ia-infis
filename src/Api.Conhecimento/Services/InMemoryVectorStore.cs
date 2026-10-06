using Api.Conhecimento.Models;

namespace Api.Conhecimento.Services;

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly object _sync = new();
    private IReadOnlyList<DocumentoChunk> _chunks = Array.Empty<DocumentoChunk>();

    public int Contagem
    {
        get
        {
            lock (_sync)
            {
                return _chunks.Count;
            }
        }
    }

    public void Substituir(IReadOnlyList<DocumentoChunk> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        lock (_sync)
        {
            _chunks = chunks.ToList();
        }
    }

    public IReadOnlyList<DocumentoChunk> Listar()
    {
        lock (_sync)
        {
            return _chunks;
        }
    }
}
