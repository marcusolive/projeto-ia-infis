using Api.Atendimento.Configuration;
using Api.Atendimento.Models;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Services;

public sealed class AtendimentoOrchestrator : IAtendimentoOrchestrator
{
    private readonly IConhecimentoMcpClient _conhecimentoMcpClient;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IOpenAiChatService _openAiChatService;
    private readonly IGeminiChatService _geminiChatService;
    private readonly IHistoricoConversaRepository _historicoRepository;
    private readonly IConsumoTokensRepository _consumoTokensRepository;
    private readonly IEncaminhamentoAtendimentoService _encaminhamentoService;
    private readonly ICustoTokenCalculator _custoTokenCalculator;
    private readonly MongoHistoricoOptions _historicoOptions;
    private readonly ILogger<AtendimentoOrchestrator> _logger;

    public AtendimentoOrchestrator(
        IConhecimentoMcpClient conhecimentoMcpClient,
        IPromptBuilder promptBuilder,
        IOpenAiChatService openAiChatService,
        IGeminiChatService geminiChatService,
        IHistoricoConversaRepository historicoRepository,
        IConsumoTokensRepository consumoTokensRepository,
        IEncaminhamentoAtendimentoService encaminhamentoService,
        ICustoTokenCalculator custoTokenCalculator,
        IOptions<MongoHistoricoOptions> historicoOptions,
        ILogger<AtendimentoOrchestrator> logger)
    {
        _conhecimentoMcpClient = conhecimentoMcpClient;
        _promptBuilder = promptBuilder;
        _openAiChatService = openAiChatService;
        _geminiChatService = geminiChatService;
        _historicoRepository = historicoRepository;
        _consumoTokensRepository = consumoTokensRepository;
        _encaminhamentoService = encaminhamentoService;
        _custoTokenCalculator = custoTokenCalculator;
        _historicoOptions = historicoOptions.Value;
        _logger = logger;
    }

    public async Task<AtendimentoResposta> ResponderAsync(
        string usuario,
        string pergunta,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(pergunta);

        _logger.LogInformation(
            "Orquestrando atendimento. Usuario={Usuario}, PerguntaChars={Tamanho}",
            usuario,
            pergunta.Length);

        var historico = await _historicoRepository.ObterUltimasPorUsuarioAsync(
            usuario,
            _historicoOptions.QuantidadeHistorico,
            cancellationToken);

        var contexto = await _conhecimentoMcpClient.BuscarConhecimentoAsync(pergunta, cancellationToken);
        var prompt = _promptBuilder.Montar(contexto, usuario, pergunta, historico);
        var (resultadoLlm, usouFallback) = await GerarComFallbackAsync(prompt, cancellationToken);

        var (custoEntrada, custoSaida, custoTotal) = _custoTokenCalculator.Calcular(
            resultadoLlm.TokensEntrada,
            resultadoLlm.TokensSaida,
            resultadoLlm.Provedor);

        var resposta = new AtendimentoResposta
        {
            Resposta = resultadoLlm.Conteudo.Resposta,
            Categoria = resultadoLlm.Conteudo.Categoria,
            Fontes = resultadoLlm.Conteudo.Fontes ?? [],
            EncaminharSuporteHumano = resultadoLlm.Conteudo.EncaminharSuporteHumano,
            ResumoEncaminhamento = resultadoLlm.Conteudo.ResumoEncaminhamento ?? string.Empty,
            Modelo = resultadoLlm.Modelo,
            Fallback = usouFallback,
            TokensEntrada = resultadoLlm.TokensEntrada,
            TokensSaida = resultadoLlm.TokensSaida,
            CustoEstimado = custoTotal
        };

        if (resposta.EncaminharSuporteHumano)
        {
            var descricao = string.IsNullOrWhiteSpace(resposta.ResumoEncaminhamento)
                ? $"Usuário solicitou/indicou necessidade de suporte humano. Pergunta: {pergunta}"
                : resposta.ResumoEncaminhamento;

            await _encaminhamentoService.EncaminharAsync(usuario, descricao, cancellationToken);
        }

        var conversa = new HistoricoConversaDocumento
        {
            DataHora = DateTime.UtcNow,
            Usuario = usuario,
            Agente = _historicoOptions.AgenteId,
            Pergunta = pergunta,
            RespostaEstruturada = resposta,
            RespostaTexto = resposta.Resposta
        };

        await _historicoRepository.SalvarAsync(conversa, cancellationToken);

        await _consumoTokensRepository.SalvarAsync(
            new ConsumoTokensRegistro
            {
                Agente = _historicoOptions.AgenteId,
                Modelo = resposta.Modelo,
                Usuario = usuario,
                DataHora = DateTimeOffset.UtcNow,
                TokensEntrada = resposta.TokensEntrada,
                TokensSaida = resposta.TokensSaida,
                CustoTokensEntrada = custoEntrada,
                CustoTokensSaida = custoSaida,
                CustoTotal = custoTotal,
                IdConversa = conversa.Id
            },
            cancellationToken);

        return resposta;
    }

    private async Task<(LlmChatResultado Resultado, bool UsouFallback)> GerarComFallbackAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        try
        {
            var resultadoOpenAi = await _openAiChatService.GerarRespostaEstruturadaAsync(
                prompt,
                cancellationToken);
            return (resultadoOpenAi, false);
        }
        catch (Exception exOpenAi) when (exOpenAi is not OperationCanceledException)
        {
            if (!_geminiChatService.EstaConfigurado)
            {
                _logger.LogError(
                    exOpenAi,
                    "OpenAI falhou e Gemini não está configurado. Não há fallback disponível.");
                throw;
            }

            _logger.LogWarning(
                exOpenAi,
                "OpenAI falhou. Acionando fallback Gemini.");

            try
            {
                var resultadoGemini = await _geminiChatService.GerarRespostaEstruturadaAsync(
                    prompt,
                    cancellationToken);
                return (resultadoGemini, true);
            }
            catch (Exception exGemini) when (exGemini is not OperationCanceledException)
            {
                _logger.LogError(
                    exGemini,
                    "Fallback Gemini também falhou após erro na OpenAI.");
                throw new AggregateException(
                    "Falha na OpenAI e no fallback Gemini.",
                    exOpenAi,
                    exGemini);
            }
        }
    }
}
