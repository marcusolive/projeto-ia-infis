using Api.Conhecimento.Configuration;
using Api.Conhecimento.Models;
using Microsoft.Extensions.Options;
using System.Text;

namespace Api.Conhecimento.Services;

public sealed class BuscaConhecimentoService : IBuscaConhecimentoService
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly ISimilarityCalculator _similarityCalculator;
    private readonly RagOptions _options;
    private readonly ILogger<BuscaConhecimentoService> _logger;

    public BuscaConhecimentoService(
        IVectorStore vectorStore,
        IEmbeddingService embeddingService,
        ISimilarityCalculator similarityCalculator,
        IOptions<RagOptions> options,
        ILogger<BuscaConhecimentoService> logger)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
        _similarityCalculator = similarityCalculator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ChunkSimilaridade>> BuscarAsync(
        string consulta,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consulta);

        var chunks = _vectorStore.Listar();
        if (chunks.Count == 0)
        {
            throw new InvalidOperationException(
                "A base vetorial em memória está vazia. Verifique a ingestão dos documentos.");
        }

        var embeddingConsulta = await _embeddingService.GerarEmbeddingAsync(consulta, cancellationToken);
        var topK = Math.Max(1, _options.TopK);

        var ranqueados = chunks
            .Select(chunk => new ChunkSimilaridade
            {
                Fonte = chunk.Fonte,
                Conteudo = chunk.Conteudo,
                Indice = chunk.Indice,
                Similaridade = _similarityCalculator.CalcularSimilaridadeCosseno(
                    embeddingConsulta,
                    chunk.Embedding)
            })
            .OrderByDescending(item => item.Similaridade)
            .Take(topK)
            .ToList();

        _logger.LogInformation(
            "Busca vetorial concluída. Consulta com {Tamanho} chars retornou {Quantidade} chunks.",
            consulta.Length,
            ranqueados.Count);

        return ranqueados;
    }

    public static string FormatResultado(IReadOnlyList<ChunkSimilaridade> chunks)
    {
        if (chunks.Count == 0)
        {
            return "Nenhum conhecimento encontrado.";
        }

        var builder = new StringBuilder();

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            builder.AppendLine($"[{i + 1}] fonte={chunk.Fonte} indice={chunk.Indice} similaridade={chunk.Similaridade:F4}");
            builder.AppendLine(chunk.Conteudo);

            if (i < chunks.Count - 1)
            {
                builder.AppendLine();
                builder.AppendLine("---");
                builder.AppendLine();
            }
        }

        return builder.ToString().TrimEnd();
    }
}
