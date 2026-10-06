using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface IAtendimentoOrchestrator
{
    Task<AtendimentoResposta> ResponderAsync(string pergunta, CancellationToken cancellationToken);
}
