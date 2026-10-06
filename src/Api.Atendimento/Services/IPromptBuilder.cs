namespace Api.Atendimento.Services;

public interface IPromptBuilder
{
    string Montar(string contexto, string pergunta);
}
