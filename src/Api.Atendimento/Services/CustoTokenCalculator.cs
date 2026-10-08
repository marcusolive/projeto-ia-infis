using Api.Atendimento.Configuration;
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
        int tokensSaida)
    {
        var custoEntrada = Math.Round(tokensEntrada * (_options.PrecoEntradaPorMilhao / 1_000_000m), 8);
        var custoSaida = Math.Round(tokensSaida * (_options.PrecoSaidaPorMilhao / 1_000_000m), 8);
        var custoTotal = Math.Round(custoEntrada + custoSaida, 8);
        return (custoEntrada, custoSaida, custoTotal);
    }
}
