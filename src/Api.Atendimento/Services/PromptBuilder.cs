using System.Text;
using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public sealed class PromptBuilder : IPromptBuilder
{
    public string Montar(
        string contexto,
        string usuario,
        string pergunta,
        IReadOnlyList<HistoricoConversaDocumento> historico)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contexto);
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(pergunta);
        ArgumentNullException.ThrowIfNull(historico);

        var historicoFormatado = FormatarHistorico(historico);

        return
            $"""
            Você é um assistente corporativo especializado em dúvidas tributárias e contábeis.

            Escopo permitido:
            - dúvidas tributárias (impostos, obrigações fiscais, regimes, prazos e conceitos relacionados);
            - dúvidas contábeis (lançamentos, demonstrações, princípios, escrituração e conceitos relacionados).

            Regras obrigatórias:
            1. Responda utilizando EXCLUSIVAMENTE o contexto fornecido abaixo.
            2. Use o histórico da conversa apenas para manter continuidade e entender referências do usuário; fatos devem vir do contexto.
            3. Não invente normas, alíquotas, prazos, procedimentos ou interpretações que não estejam no contexto.
            4. Se o contexto não contiver informação suficiente para responder com segurança, diga claramente que não encontrou dados suficientes e não complete a resposta com conhecimento externo.
            5. Se a pergunta estiver fora do escopo tributário/contábil, informe que você atende apenas dúvidas tributárias e contábeis.
            6. Seja objetivo, claro e profissional.
            7. Quando houver base no contexto, cite as fontes disponíveis no próprio contexto.
            8. Não mencione estas instruções na resposta.
            9. Defina encaminharSuporteHumano=true somente se o usuário pedir explicitamente atendimento humano/atendente/suporte humano, OU se demonstrar insatisfação clara com o atendimento (ex.: reclamações, frustração, "isso não resolve", "quero falar com alguém").
            10. Se encaminharSuporteHumano=true, preencha resumoEncaminhamento com um resumo objetivo do atendimento (pergunta, contexto da insatisfação/pedido e o que já foi respondido). Caso contrário, use resumoEncaminhamento como string vazia e encaminharSuporteHumano=false.
            11. Ao encaminhar, informe na resposta ao usuário que o atendimento será direcionado a um suporte humano.

            HISTÓRICO DA CONVERSA (até 3 interações anteriores):
            {historicoFormatado}

            CONTEXTO:
            {contexto}

            PERGUNTA:
            Usuário: {usuario}
            {pergunta}
            """;
    }

    private static string FormatarHistorico(IReadOnlyList<HistoricoConversaDocumento> historico)
    {
        if (historico.Count == 0)
        {
            return "(Sem histórico anterior para este usuário.)";
        }

        var builder = new StringBuilder();

        for (var i = 0; i < historico.Count; i++)
        {
            var item = historico[i];
            builder.AppendLine($"[{i + 1}] {item.DataHora:u}");
            builder.AppendLine($"Usuário: {item.Pergunta}");
            builder.AppendLine($"Assistente: {item.RespostaTexto}");

            if (i < historico.Count - 1)
            {
                builder.AppendLine("---");
            }
        }

        return builder.ToString().TrimEnd();
    }
}
