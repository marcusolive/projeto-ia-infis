using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Atendimento.Models;

public sealed class AtendimentoResposta
{
    [BsonElement("resposta")]
    [JsonPropertyName("resposta")]
    public string Resposta { get; set; } = string.Empty;

    [BsonElement("categoria")]
    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;

    [BsonElement("fontes")]
    [JsonPropertyName("fontes")]
    public string[] Fontes { get; set; } = [];

    [BsonElement("modelo")]
    [JsonPropertyName("modelo")]
    public string Modelo { get; set; } = string.Empty;

    [BsonElement("fallback")]
    [JsonPropertyName("fallback")]
    public bool Fallback { get; set; }

    [BsonElement("tokensEntrada")]
    [JsonPropertyName("tokensEntrada")]
    public int TokensEntrada { get; set; }

    [BsonElement("tokensSaida")]
    [JsonPropertyName("tokensSaida")]
    public int TokensSaida { get; set; }

    [BsonElement("custoEstimado")]
    [JsonPropertyName("custoEstimado")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Decimal128)]
    public decimal CustoEstimado { get; set; }
}
