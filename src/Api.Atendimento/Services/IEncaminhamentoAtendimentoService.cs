namespace Api.Atendimento.Services;

public interface IEncaminhamentoAtendimentoService
{
    Task EncaminharAsync(string usuario, string descricao, CancellationToken cancellationToken);
}
