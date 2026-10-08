using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface IConsumoTokensRepository
{
    Task SalvarAsync(ConsumoTokensRegistro registro, CancellationToken cancellationToken);
}
