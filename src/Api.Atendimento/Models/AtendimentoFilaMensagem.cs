using System.Text.Json.Serialization;

namespace Api.Atendimento.Models;

public sealed class AtendimentoFilaMensagem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("usuario")]
    public string Usuario { get; set; } = string.Empty;

    [JsonPropertyName("descricao")]
    public string Descricao { get; set; } = string.Empty;
}
