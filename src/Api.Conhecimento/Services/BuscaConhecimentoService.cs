using Api.Conhecimento.Configuration;
using Api.Conhecimento.Models;
using Microsoft.Extensions.Options;
using System.Text;

namespace Api.Conhecimento.Services;

public sealed class BuscaConhecimentoService : IBuscaConhecimentoService
{
    private readonly IConhecimentoChunkRepository _chunkRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly RagOptions _options;
    private readonly ILogger<BuscaConhecimentoService> _logger;

    public BuscaConhecimentoService(
        IConhecimentoChunkRepository chunkRepository,
        IEmbeddingService embeddingService,
        IOptions<RagOptions> options,
        ILogger<BuscaConhecimentoService> logger)
    {
        _chunkRepository = chunkRepository;
        _embeddingService = embeddingService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ChunkSimilaridade>> BuscarAsync(
        string consulta,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consulta);

        var embeddingConsulta = await _embeddingService.GerarEmbeddingAsync(consulta, cancellationToken);
        var topK = Math.Max(1, _options.TopK);

        var ranqueados = await _chunkRepository.BuscarPorSimilaridadeAsync(
            _options.AgenteIdPadrao,
            embeddingConsulta,
            topK,
            cancellationToken);

        if (ranqueados.Count == 0)
        {
            _logger.LogWarning("Nenhum chunk válido encontrado na base RAG para a consulta.");
        }
        else
        {
            _logger.LogInformation(
                "Busca vetorial RAG concluída. Consulta com {Tamanho} chars retornou {Quantidade} chunks.",
                consulta.Length,
                ranqueados.Count);
        }

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