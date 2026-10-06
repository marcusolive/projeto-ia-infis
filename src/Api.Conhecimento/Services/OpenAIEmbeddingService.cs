using Api.Conhecimento.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;

namespace Api.Conhecimento.Services;

public sealed class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<OpenAIEmbeddingService> _logger;

    public OpenAIEmbeddingService(
        OpenAIClient openAIClient,
        IOptions<OpenAIOptions> options,
        ILogger<OpenAIEmbeddingService> logger)
    {
        _embeddingClient = openAIClient.GetEmbeddingClient(options.Value.EmbeddingModel);
        _logger = logger;
    }

    public async Task<float[]> GerarEmbeddingAsync(string texto, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);

        var resultado = await _embeddingClient.GenerateEmbeddingAsync(
            texto,
            cancellationToken: cancellationToken);

        return resultado.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GerarEmbeddingsAsync(
        IReadOnlyList<string> textos,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(textos);

        if (textos.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        var resultado = await _embeddingClient.GenerateEmbeddingsAsync(
            textos,
            cancellationToken: cancellationToken);

        var embeddings = resultado.Value
            .OrderBy(item => item.Index)
            .Select(item => item.ToFloats().ToArray())
            .ToList();

        _logger.LogInformation(
            "Gerados {Quantidade} embeddings com o modelo configurado.",
            embeddings.Count);

        return embeddings;
    }
}
