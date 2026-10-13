using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Atendimento.Configuration;
using Api.Atendimento.Models;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Services;

public sealed class GeminiChatService : IGeminiChatService, IAsyncDisposable
{
    private static readonly JsonNode Schema = JsonNode.Parse(LlmRespostaSchema.Json)!;

    private readonly Client? _client;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiChatService> _logger;
    private readonly bool _usandoVertexAi;

    public GeminiChatService(
        IOptions<GeminiOptions> options,
        ILogger<GeminiChatService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _usandoVertexAi = DeveUsarVertexAi(_options);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _client = null;
            return;
        }

        _client = _usandoVertexAi
            ? new Client(vertexAI: true, apiKey: _options.ApiKey)
            : new Client(apiKey: _options.ApiKey);

        _logger.LogInformation(
            "Cliente Gemini inicializado. Modelo={Modelo}, VertexAiExpress={VertexAi}.",
            _options.ChatModel,
            _usandoVertexAi);
    }

    public bool EstaConfigurado => _client is not null;

    public async Task<LlmChatResultado> GerarRespostaEstruturadaAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        if (_client is null)
        {
            throw new InvalidOperationException(
                "Gemini não está configurado. Defina Gemini:ApiKey ou a variável GEMINI_API_KEY.");
        }

        var config = new GenerateContentConfig
        {
            ResponseMimeType = "application/json",
            ResponseJsonSchema = Schema,
            SystemInstruction = new Content
            {
                Parts =
                [
                    new Part
                    {
                        Text = LlmRespostaSchema.InstrucaoSistema
                    }
                ]
            }
        };

        try
        {
            var response = await _client.Models.GenerateContentAsync(
                model: _options.ChatModel,
                contents: prompt,
                config: config,
                cancellationToken: cancellationToken);

            var json = response.Text;
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("O Gemini não retornou conteúdo textual estruturado.");
            }

            var conteudo = JsonSerializer.Deserialize<LlmConteudoEstruturado>(json)
                ?? throw new InvalidOperationException("Falha ao desserializar a resposta estruturada do Gemini.");

            var tokensEntrada = response.UsageMetadata?.PromptTokenCount ?? 0;
            var tokensSaida = response.UsageMetadata?.CandidatesTokenCount ?? 0;

            _logger.LogInformation(
                "Gemini respondeu com modelo {Modelo} (VertexAi={VertexAi}). Tokens entrada={Entrada}, saída={Saida}.",
                _options.ChatModel,
                _usandoVertexAi,
                tokensEntrada,
                tokensSaida);

            return new LlmChatResultado(
                conteudo,
                _options.ChatModel,
                tokensEntrada,
                tokensSaida,
                LlmProvedor.Gemini);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not InvalidOperationException)
        {
            _logger.LogError(
                ex,
                "Falha ao chamar Gemini. Modelo={Modelo}, VertexAi={VertexAi}.",
                _options.ChatModel,
                _usandoVertexAi);
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_client is null)
        {
            return ValueTask.CompletedTask;
        }

        return _client.DisposeAsync();
    }

    private static bool DeveUsarVertexAi(GeminiOptions options)
    {
        if (options.UseVertexAi.HasValue)
        {
            return options.UseVertexAi.Value;
        }

        // Chaves do Google AI Studio começam com "AIza".
        // Chaves do Express Mode / Vertex AI costumam começar com "AQ.".
        return options.ApiKey.StartsWith("AQ.", StringComparison.Ordinal);
    }
}
