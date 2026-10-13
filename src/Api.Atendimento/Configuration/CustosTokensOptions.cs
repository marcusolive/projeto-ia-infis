namespace Api.Atendimento.Configuration;

public sealed class CustosTokensOptions
{
    public const string SectionName = "CustosTokens";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Preço USD por 1 milhão de tokens de entrada (OpenAI).</summary>
    public decimal PrecoEntradaPorMilhao { get; set; } = 0.15m;

    /// <summary>Preço USD por 1 milhão de tokens de saída (OpenAI).</summary>
    public decimal PrecoSaidaPorMilhao { get; set; } = 0.60m;

    /// <summary>Preço USD por 1 milhão de tokens de entrada (Gemini).</summary>
    public decimal GeminiPrecoEntradaPorMilhao { get; set; } = 0.30m;

    /// <summary>Preço USD por 1 milhão de tokens de saída (Gemini).</summary>
    public decimal GeminiPrecoSaidaPorMilhao { get; set; } = 2.50m;
}
