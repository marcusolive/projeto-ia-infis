namespace Api.Conhecimento.Services;

public interface IEmbeddingService
{
    Task<float[]> GerarEmbeddingAsync(string texto, CancellationToken cancellationToken);

    Task<IReadOnlyList<float[]>> GerarEmbeddingsAsync(
        IReadOnlyList<string> textos,
        CancellationToken cancellationToken);
}
