using System.ClientModel;
using System.Text.Json;
using Api.Atendimento.Configuration;
using Api.Atendimento.Models;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Api.Atendimento.Services;

public sealed class OpenAiChatService : IOpenAiChatService
{
    private static readonly BinaryData Schema = BinaryData.FromString(LlmRespostaSchema.Json);

    private readonly ChatClient _chatClient;
    private readonly OpenAIOptions _options;
    private readonly ILogger<OpenAiChatService> _logger;

    public OpenAiChatService(
        OpenAIClient openAIClient,
        IOptions<OpenAIOptions> options,
        ILogger<OpenAiChatService> logger)
    {
        _options = options.Value;
        _chatClient = openAIClient.GetChatClient(_options.ChatModel);
        _logger = logger;
    }

    public async Task<LlmChatResultado> GerarRespostaEstruturadaAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(LlmRespostaSchema.InstrucaoSistema),
            new UserChatMessage(prompt)
        };

        var chatOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "atendimento_resposta",
                jsonSchema: Schema,
                jsonSchemaIsStrict: true)
        };

        ClientResult<ChatCompletion> completionResult = await _chatClient.CompleteChatAsync(
            messages,
            chatOptions,
            cancellationToken);

        ChatCompletion completion = completionResult.Value;

        if (completion.Content.Count == 0 || string.IsNullOrWhiteSpace(completion.Content[0].Text))
        {
            throw new InvalidOperationException("A OpenAI não retornou conteúdo textual estruturado.");
        }

        var json = completion.Content[0].Text;
        var conteudo = JsonSerializer.Deserialize<LlmConteudoEstruturado>(json)
            ?? throw new InvalidOperationException("Falha ao desserializar a resposta estruturada da OpenAI.");

        var tokensEntrada = completion.Usage?.InputTokenCount ?? 0;
        var tokensSaida = completion.Usage?.OutputTokenCount ?? 0;

        _logger.LogInformation(
            "OpenAI respondeu com modelo {Modelo}. Tokens entrada={Entrada}, saída={Saida}.",
            _options.ChatModel,
            tokensEntrada,
            tokensSaida);

        return new LlmChatResultado(
            conteudo,
            _options.ChatModel,
            tokensEntrada,
            tokensSaida,
            LlmProvedor.OpenAi);
    }
}
