namespace Worker.Atendimento.Services;

public interface IEmailNotificacaoService
{
    Task EnviarAtendimentoAsync(string usuario, string descricao, CancellationToken cancellationToken);
}
