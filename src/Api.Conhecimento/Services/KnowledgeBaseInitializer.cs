using Api.Conhecimento.Models;

namespace Api.Conhecimento.Services;

public sealed class KnowledgeBaseInitializer : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KnowledgeBaseInitializer> _logger;

    public KnowledgeBaseInitializer(
        IServiceScopeFactory scopeFactory,
        ILogger<KnowledgeBaseInitializer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var documentLoader = scope.ServiceProvider.GetRequiredService<IDocumentLoader>();
        var chunkingService = scope.ServiceProvider.GetRequiredService<IChunkingService>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();

        _logger.LogInformation("Iniciando ingestão da base de conhecimento em memória.");

        var documentos = await documentLoader.CarregarMarkdownAsync(cancellationToken);
        var textosChunks = documentos
            .SelectMany(documento => chunkingService.Dividir(documento.NomeArquivo, documento.Conteudo))
            .ToList();

        if (textosChunks.Count == 0)
        {
            throw new InvalidOperationException("Nenhum chunk foi gerado a partir dos documentos.");
        }

        var embeddings = await embeddingService.GerarEmbeddingsAsync(
            textosChunks.Select(chunk => chunk.Conteudo).ToList(),
            cancellationToken);

        if (embeddings.Count != textosChunks.Count)
        {
            throw new InvalidOperationException(
                $"Quantidade de embeddings ({embeddings.Count}) diferente da quantidade de chunks ({textosChunks.Count}).");
        }

        var chunks = new List<DocumentoChunk>(textosChunks.Count);

        for (var i = 0; i < textosChunks.Count; i++)
        {
            var textoChunk = textosChunks[i];
            chunks.Add(new DocumentoChunk
            {
                Id = $"{textoChunk.Fonte}:{textoChunk.Indice}",
                Fonte = textoChunk.Fonte,
                Conteudo = textoChunk.Conteudo,
                Indice = textoChunk.Indice,
                Embedding = embeddings[i]
            });
        }

        vectorStore.Substituir(chunks);

        _logger.LogInformation(
            "Base de conhecimento carregada: {Documentos} documentos, {Chunks} chunks em memória.",
            documentos.Count,
            chunks.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
