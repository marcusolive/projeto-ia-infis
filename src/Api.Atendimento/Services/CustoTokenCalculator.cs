using Api.Atendimento.Configuration;
using Api.Atendimento.Models;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Services;

public sealed class CustoTokenCalculator : ICustoTokenCalculator
{
    private readonly CustosTokensOptions _options;

    public CustoTokenCalculator(IOptions<CustosTokensOptions> options)
    {
        _options = options.Value;
    }

    public (decimal CustoEntrada, decimal CustoSaida, decimal CustoTotal) Calcular(
        int tokensEntrada,
        int tokensSaida,
        LlmProvedor provedor)
    {
        var (precoEntrada, precoSaida) = provedor switch
        {
            LlmProvedor.Gemini => (
                _options.GeminiPrecoEntradaPorMilhao,
                _options.GeminiPrecoSaidaPorMilhao),
            _ => (
                _options.PrecoEntradaPorMilhao,
                _options.PrecoSaidaPorMilhao)
        };

        var custoEntrada = Math.Round(tokensEntrada * (precoEntrada / 1_000_000m), 8);
        var custoSaida = Math.Round(tokensSaida * (precoSaida / 1_000_000m), 8);
        var custoTotal = Math.Round(custoEntrada + custoSaida, 8);
        return (custoEntrada, custoSaida, custoTotal);
    }
}
