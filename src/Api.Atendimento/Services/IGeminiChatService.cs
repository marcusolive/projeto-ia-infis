using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface IGeminiChatService
{
    bool EstaConfigurado { get; }

    Task<LlmChatResultado> GerarRespostaEstruturadaAsync(
        string prompt,
        CancellationToken cancellationToken);
}
