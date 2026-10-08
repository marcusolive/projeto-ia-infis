using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Atendimento.Models;

public sealed class HistoricoConversaDocumento
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("dataHora")]
    public DateTime DataHora { get; set; } = DateTime.UtcNow;

    [BsonElement("usuario")]
    public string Usuario { get; set; } = string.Empty;

    [BsonElement("agente")]
    public string Agente { get; set; } = string.Empty;

    [BsonElement("pergunta")]
    public string Pergunta { get; set; } = string.Empty;

    [BsonElement("respostaEstruturada")]
    public AtendimentoResposta RespostaEstruturada { get; set; } = null!;

    [BsonElement("respostaTexto")]
    public string RespostaTexto { get; set; } = string.Empty;
}
