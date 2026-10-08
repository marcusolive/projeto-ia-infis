namespace Api.Atendimento.Configuration;

public sealed class MongoHistoricoOptions
{
    public const string SectionName = "MongoHistorico";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "bd-historico-atendimentos";
    public string CollectionName { get; set; } = "conversas";
    public string AgenteId { get; set; } = "assistente-tributario-contabil";
    public int QuantidadeHistorico { get; set; } = 3;
}
