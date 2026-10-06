using System.Text.Json.Serialization;

namespace Api.Atendimento.Models;

public sealed class AtendimentoResposta
{
    [JsonPropertyName("resposta")]
    public required string Resposta { get; init; }

    [JsonPropertyName("categoria")]
    public required string Categoria { get; init; }

    [JsonPropertyName("fontes")]
    public required string[] Fontes { get; init; }

    [JsonPropertyName("modelo")]
    public required string Modelo { get; init; }

    [JsonPropertyName("fallback")]
    public required bool Fallback { get; init; }

    [JsonPropertyName("tokensEntrada")]
    public required int TokensEntrada { get; init; }

    [JsonPropertyName("tokensSaida")]
    public required int TokensSaida { get; init; }

    [JsonPropertyName("custoEstimado")]
    public required decimal CustoEstimado { get; init; }
}
