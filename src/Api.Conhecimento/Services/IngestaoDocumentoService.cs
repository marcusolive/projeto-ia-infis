using Api.Conhecimento.Configuration;
using Api.Conhecimento.Models;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Services;

public sealed class IngestaoDocumentoService : IIngestaoDocumentoService
{
    private static readonly byte[] PdfMagic = "%PDF"u8.ToArray();

    private readonly RagOptions _ragOptions;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IConhecimentoChunkRepository _chunkRepository;
    private readonly ILogger<IngestaoDocumentoService> _logger;

    public IngestaoDocumentoService(
        IOptions<RagOptions> ragOptions,
        IPdfTextExtractor pdfTextExtractor,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IConhecimentoChunkRepository chunkRepository,
        ILogger<IngestaoDocumentoService> logger)
    {
        _ragOptions = ragOptions.Value;
        _pdfTextExtractor = pdfTextExtractor;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _chunkRepository = chunkRepository;
        _logger = logger;
    }

    public async Task IngerirAsync(IngestaoDocumentoRequest request, CancellationToken cancellationToken)
    {
        ValidarMetadados(request);
        await ValidarArquivoPdfAsync(request.Arquivo, cancellationToken);

        _logger.LogInformation(
            "Ingestão iniciada. Usuario={Usuario}, Documento={Documento}, Arquivo={Arquivo}",
            request.Usuario,
            request.CodigoDocumento,
            request.Arquivo.FileName);

        await using var stream = request.Arquivo.OpenReadStream();
        var texto = await _pdfTextExtractor.ExtrairTextoAsync(stream, cancellationToken);
        _logger.LogInformation("Texto extraído do PDF ({Tamanho} chars).", texto.Length);

        var fonte = Path.GetFileName(request.Arquivo.FileName);
        var chunks = _chunkingService.Dividir(fonte, texto);
        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("Nenhum chunk foi gerado a partir do PDF.");
        }

        _logger.LogInformation("Documento dividido em {Quantidade} chunks.", chunks.Count);

        var embeddings = await _embeddingService.GerarEmbeddingsAsync(
            chunks.Select(c => c.Conteudo).ToList(),
            cancellationToken);

        if (embeddings.Count != chunks.Count)
        {
            throw new InvalidOperationException(
                $"Quantidade de embeddings ({embeddings.Count}) diferente da quantidade de chunks ({chunks.Count}).");
        }

        _logger.LogInformation("Embeddings gerados para {Quantidade} chunks.", embeddings.Count);

        var agenteId = _ragOptions.AgenteIdPadrao;
        var registros = new List<ChunkParaPersistencia>(chunks.Count);

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            registros.Add(new ChunkParaPersistencia
            {
                AgenteId = agenteId,
                DocumentoCodigo = request.CodigoDocumento.Trim(),
                DocumentoNome = fonte,
                ValidoAte = request.Validade,
                Usuario = request.Usuario.Trim(),
                Conteudo = chunk.Conteudo,
                Embedding = embeddings[i],
                IndiceChunk = chunk.Indice
            });
        }

        await _chunkRepository.SubstituirDocumentoAsync(
            agenteId,
            request.CodigoDocumento.Trim(),
            registros,
            cancellationToken);

        _logger.LogInformation(
            "Ingestão concluída. Documento={Documento}, Chunks={Chunks}",
            request.CodigoDocumento,
            registros.Count);
    }

    private static void ValidarMetadados(IngestaoDocumentoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario))
        {
            throw new ArgumentException("O nome do usuário é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.CodigoDocumento))
        {
            throw new ArgumentException("O código do documento é obrigatório.");
        }

        if (request.Validade == default)
        {
            throw new ArgumentException("A validade do documento é obrigatória.");
        }
    }

    private async Task ValidarArquivoPdfAsync(IFormFile arquivo, CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length <= 0)
        {
            throw new ArgumentException("O arquivo PDF é obrigatório.");
        }

        if (arquivo.Length > _ragOptions.TamanhoMaximoPdfBytes)
        {
            throw new ArgumentException(
                $"O PDF deve ter no máximo {_ragOptions.TamanhoMaximoPdfBytes / (1024 * 1024)}MB.");
        }

        var extensao = Path.GetExtension(arquivo.FileName);
        var contentType = arquivo.ContentType ?? string.Empty;
        var extensaoValida = string.Equals(extensao, ".pdf", StringComparison.OrdinalIgnoreCase);
        var contentTypeValido = contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase);

        if (!extensaoValida && !contentTypeValido)
        {
            throw new ArgumentException("O arquivo enviado deve ser um PDF.");
        }

        await using var stream = arquivo.OpenReadStream();
        var header = new byte[PdfMagic.Length];
        var lidos = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

        if (lidos < PdfMagic.Length || !header.AsSpan().SequenceEqual(PdfMagic))
        {
            throw new ArgumentException("O conteúdo do arquivo não corresponde a um PDF válido.");
        }
    }
}
