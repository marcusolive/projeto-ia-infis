namespace Api.Atendimento.Services;

public sealed class PromptBuilder : IPromptBuilder
{
    public string Montar(string contexto, string pergunta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contexto);
        ArgumentException.ThrowIfNullOrWhiteSpace(pergunta);

        return
            $"""
            Você é um agente corporativo.

            Responda utilizando exclusivamente
            o contexto fornecido.

            Caso o contexto não contenha a informação,
            informe que não encontrou dados suficientes.

            CONTEXTO:

            {contexto}

            PERGUNTA:

            {pergunta}
            """;
    }
}
