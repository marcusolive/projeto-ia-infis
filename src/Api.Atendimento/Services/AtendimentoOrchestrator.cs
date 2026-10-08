using Api.Atendimento.Configuration;
using Api.Atendimento.Models;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Services;

public sealed class AtendimentoOrchestrator : IAtendimentoOrchestrator
{
    private readonly IConhecimentoMcpClient _conhecimentoMcpClient;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IOpenAiChatService _openAiChatService;
    private readonly IHistoricoConversaRepository _historicoRepository;
    private readonly IConsumoTokensRepository _consumoTokensRepository;
    private readonly ICustoTokenCalculator _custoTokenCalculator;
    private readonly MongoHistoricoOptions _historicoOptions;
    private readonly ILogger<AtendimentoOrchestrator> _logger;

    public AtendimentoOrchestrator(
        IConhecimentoMcpClient conhecimentoMcpClient,
        IPromptBuilder promptBuilder,
        IOpenAiChatService openAiChatService,
        IHistoricoConversaRepository historicoRepository,
        IConsumoTokensRepository consumoTokensRepository,
        ICustoTokenCalculator custoTokenCalculator,
        IOptions<MongoHistoricoOptions> historicoOptions,
        ILogger<AtendimentoOrchestrator> logger)
    {
        _conhecimentoMcpClient = conhecimentoMcpClient;
        _promptBuilder = promptBuilder;
        _openAiChatService = openAiChatService;
        _historicoRepository = historicoRepository;
        _consumoTokensRepository = consumoTokensRepository;
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
        var resultadoLlm = await _openAiChatService.GerarRespostaEstruturadaAsync(prompt, cancellationToken);

        var (custoEntrada, custoSaida, custoTotal) = _custoTokenCalculator.Calcular(
            resultadoLlm.TokensEntrada,
            resultadoLlm.TokensSaida);

        var resposta = new AtendimentoResposta
        {
            Resposta = resultadoLlm.Conteudo.Resposta,
            Categoria = resultadoLlm.Conteudo.Categoria,
            Fontes = resultadoLlm.Conteudo.Fontes ?? [],
            Modelo = resultadoLlm.Modelo,
            Fallback = false,
            TokensEntrada = resultadoLlm.TokensEntrada,
            TokensSaida = resultadoLlm.TokensSaida,
            CustoEstimado = custoTotal
        };

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
}
