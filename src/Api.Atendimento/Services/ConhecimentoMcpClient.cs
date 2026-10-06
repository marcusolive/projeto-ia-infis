using Api.Atendimento.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Api.Atendimento.Services;

public sealed class ConhecimentoMcpClient : IConhecimentoMcpClient
{
    private const string ToolName = "BuscarConhecimento";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly McpConhecimentoOptions _options;
    private readonly ILogger<ConhecimentoMcpClient> _logger;

    public ConhecimentoMcpClient(
        IHttpClientFactory httpClientFactory,
        IOptions<McpConhecimentoOptions> options,
        ILogger<ConhecimentoMcpClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<McpClientTool>> ListarToolsAsync(CancellationToken cancellationToken)
    {
        await using var client = await CreateClientAsync(cancellationToken);

        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Tools MCP descobertas em {Endpoint}: {Tools}",
            _options.Endpoint,
            string.Join(", ", tools.Select(tool => tool.Name)));

        return tools.ToList();
    }

    public async Task<string> BuscarConhecimentoAsync(string consulta, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consulta);

        await using var client = await CreateClientAsync(cancellationToken);

        var result = await client.CallToolAsync(
            ToolName,
            new Dictionary<string, object?>
            {
                ["consulta"] = consulta
            },
            cancellationToken: cancellationToken);

        var texto = ExtractText(result);

        _logger.LogInformation(
            "Tool {ToolName} chamada com sucesso. Resultado: {Resultado}",
            ToolName,
            texto);

        return texto;
    }

    private async Task<McpClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new InvalidOperationException(
                $"Configuração '{McpConhecimentoOptions.SectionName}:Endpoint' não definida.");
        }

        var httpClient = _httpClientFactory.CreateClient("McpConhecimento");

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(_options.Endpoint),
                TransportMode = HttpTransportMode.StreamableHttp,
                Name = "Api.Conhecimento"
            },
            httpClient,
            ownsHttpClient: false);

        return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    private static string ExtractText(CallToolResult result)
    {
        var texto = result.Content
            .OfType<TextContentBlock>()
            .Select(block => block.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        if (texto is null)
        {
            throw new InvalidOperationException(
                $"A tool '{ToolName}' não retornou conteúdo textual.");
        }

        return texto;
    }
}
