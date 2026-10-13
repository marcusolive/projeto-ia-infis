using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface IOpenAiChatService
{
    Task<LlmChatResultado> GerarRespostaEstruturadaAsync(
        string prompt,
        CancellationToken cancellationToken);
}

public sealed record LlmChatResultado(
    LlmConteudoEstruturado Conteudo,
    string Modelo,
    int TokensEntrada,
    int TokensSaida,
    LlmProvedor Provedor);
