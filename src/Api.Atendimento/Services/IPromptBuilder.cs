using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface IPromptBuilder
{
    string Montar(
        string contexto,
        string usuario,
        string pergunta,
        IReadOnlyList<HistoricoConversaDocumento> historico);
}
