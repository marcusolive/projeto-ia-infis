using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface IAtendimentoOrchestrator
{
    Task<AtendimentoResposta> ResponderAsync(
        string usuario,
        string pergunta,
        CancellationToken cancellationToken);
}
