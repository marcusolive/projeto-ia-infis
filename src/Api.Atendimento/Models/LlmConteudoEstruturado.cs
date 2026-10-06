using System.Text.Json.Serialization;

namespace Api.Atendimento.Models;

public sealed class LlmConteudoEstruturado
{
    [JsonPropertyName("resposta")]
    public string Resposta { get; set; } = string.Empty;

    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("fontes")]
    public string[] Fontes { get; set; } = [];
}
