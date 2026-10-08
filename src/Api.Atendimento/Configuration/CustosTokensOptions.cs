namespace Api.Atendimento.Configuration;

public sealed class CustosTokensOptions
{
    public const string SectionName = "CustosTokens";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Preço USD por 1 milhão de tokens de entrada.</summary>
    public decimal PrecoEntradaPorMilhao { get; set; } = 0.15m;

    /// <summary>Preço USD por 1 milhão de tokens de saída.</summary>
    public decimal PrecoSaidaPorMilhao { get; set; } = 0.60m;
}
