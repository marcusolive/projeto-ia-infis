using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface IOpenAiChatService
{
    Task<OpenAiChatResultado> GerarRespostaEstruturadaAsync(
        string prompt,
        CancellationToken cancellationToken);
}

public sealed record OpenAiChatResultado(
    LlmConteudoEstruturado Conteudo,
    string Modelo,
    int TokensEntrada,
    int TokensSaida);
