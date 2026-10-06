using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public sealed class AtendimentoOrchestrator : IAtendimentoOrchestrator
{
    private readonly IConhecimentoMcpClient _conhecimentoMcpClient;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IOpenAiChatService _openAiChatService;
    private readonly ILogger<AtendimentoOrchestrator> _logger;

    public AtendimentoOrchestrator(
        IConhecimentoMcpClient conhecimentoMcpClient,
        IPromptBuilder promptBuilder,
        IOpenAiChatService openAiChatService,
        ILogger<AtendimentoOrchestrator> logger)
    {
        _conhecimentoMcpClient = conhecimentoMcpClient;
        _promptBuilder = promptBuilder;
        _openAiChatService = openAiChatService;
        _logger = logger;
    }

    public async Task<AtendimentoResposta> ResponderAsync(
        string pergunta,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pergunta);

        _logger.LogInformation("Orquestrando atendimento para pergunta com {Tamanho} chars.", pergunta.Length);

        var contexto = await _conhecimentoMcpClient.BuscarConhecimentoAsync(pergunta, cancellationToken);
        var prompt = _promptBuilder.Montar(contexto, pergunta);
        var resultadoLlm = await _openAiChatService.GerarRespostaEstruturadaAsync(prompt, cancellationToken);

        return new AtendimentoResposta
        {
            Resposta = resultadoLlm.Conteudo.Resposta,
            Categoria = resultadoLlm.Conteudo.Categoria,
            Fontes = resultadoLlm.Conteudo.Fontes ?? [],
            Modelo = resultadoLlm.Modelo,
            Fallback = false,
            TokensEntrada = resultadoLlm.TokensEntrada,
            TokensSaida = resultadoLlm.TokensSaida,
            CustoEstimado = 0m
        };
    }
}
