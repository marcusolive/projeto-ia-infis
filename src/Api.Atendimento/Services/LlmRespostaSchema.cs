namespace Api.Atendimento.Services;

internal static class LlmRespostaSchema
{
    public const string Json = """
        {
          "type": "object",
          "properties": {
            "resposta": { "type": "string" },
            "categoria": { "type": "string" },
            "fontes": {
              "type": "array",
              "items": { "type": "string" }
            },
            "encaminharSuporteHumano": { "type": "boolean" },
            "resumoEncaminhamento": { "type": "string" }
          },
          "required": ["resposta", "categoria", "fontes", "encaminharSuporteHumano", "resumoEncaminhamento"],
          "additionalProperties": false
        }
        """;

    public const string InstrucaoSistema =
        "Você é um assistente tributário e contábil. " +
        "Responda apenas com base no contexto. " +
        "Preencha 'fontes' com os nomes dos documentos citados no contexto. " +
        "Use categoria 'tributaria', 'contabil', 'fora_escopo' ou 'sem_dados'. " +
        "Defina encaminharSuporteHumano=true somente quando: " +
        "(1) o usuário pedir explicitamente atendimento humano/atendente/suporte humano, ou " +
        "(2) o usuário demonstrar insatisfação clara com o atendimento. " +
        "Se encaminharSuporteHumano=true, preencha resumoEncaminhamento com um resumo objetivo do atendimento; " +
        "caso contrário, use resumoEncaminhamento como string vazia.";
}
