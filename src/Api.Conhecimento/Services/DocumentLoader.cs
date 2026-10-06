using Api.Conhecimento.Configuration;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Services;

public sealed class DocumentLoader : IDocumentLoader
{
    private readonly RagOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DocumentLoader> _logger;

    public DocumentLoader(
        IOptions<RagOptions> options,
        IHostEnvironment environment,
        ILogger<DocumentLoader> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DocumentoCarregado>> CarregarMarkdownAsync(CancellationToken cancellationToken)
    {
        var pasta = Path.IsPathRooted(_options.DocumentosPath)
            ? _options.DocumentosPath
            : Path.Combine(_environment.ContentRootPath, _options.DocumentosPath);

        if (!Directory.Exists(pasta))
        {
            throw new DirectoryNotFoundException(
                $"Pasta de documentos não encontrada: '{pasta}'.");
        }

        var arquivos = Directory.GetFiles(pasta, "*.md", SearchOption.TopDirectoryOnly);
        var documentos = new List<DocumentoCarregado>(arquivos.Length);

        foreach (var arquivo in arquivos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var conteudo = await File.ReadAllTextAsync(arquivo, cancellationToken);
            var nome = Path.GetFileName(arquivo);

            if (string.IsNullOrWhiteSpace(conteudo))
            {
                _logger.LogWarning("Arquivo Markdown vazio ignorado: {Arquivo}", nome);
                continue;
            }

            documentos.Add(new DocumentoCarregado(nome, conteudo));
            _logger.LogInformation("Documento carregado: {Arquivo} ({Tamanho} chars)", nome, conteudo.Length);
        }

        if (documentos.Count == 0)
        {
            throw new InvalidOperationException(
                $"Nenhum arquivo .md válido encontrado em '{pasta}'.");
        }

        return documentos;
    }
}
