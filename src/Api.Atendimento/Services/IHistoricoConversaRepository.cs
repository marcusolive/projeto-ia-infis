using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface IHistoricoConversaRepository
{
    Task<IReadOnlyList<HistoricoConversaDocumento>> ObterUltimasPorUsuarioAsync(
        string usuario,
        int quantidade,
        CancellationToken cancellationToken);

    Task SalvarAsync(HistoricoConversaDocumento documento, CancellationToken cancellationToken);
}
