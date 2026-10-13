using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface IOutboxMessageRepository
{
    Task<long> SalvarAsync(OutboxMessage mensagem, CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxMessage>> ObterPendentesAsync(int limite, CancellationToken cancellationToken);

    Task MarcarComoTransmitidoAsync(long id, string nomeFila, CancellationToken cancellationToken);
}
