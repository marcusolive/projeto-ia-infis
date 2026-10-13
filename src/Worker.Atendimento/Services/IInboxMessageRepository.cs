using Worker.Atendimento.Models;

namespace Worker.Atendimento.Services;

public interface IInboxMessageRepository
{
    Task<long> SalvarAsync(InboxMessage mensagem, CancellationToken cancellationToken);
}
