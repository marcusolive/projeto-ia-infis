namespace Api.Atendimento.Models;

public sealed class ConsumoTokensRegistro
{
    public required string Agente { get; init; }
    public required string Modelo { get; init; }
    public required string Usuario { get; init; }
    public required DateTimeOffset DataHora { get; init; }
    public required int TokensEntrada { get; init; }
    public required int TokensSaida { get; init; }
    public required decimal CustoTokensEntrada { get; init; }
    public required decimal CustoTokensSaida { get; init; }
    public required decimal CustoTotal { get; init; }
    public required string IdConversa { get; init; }
}
