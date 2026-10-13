using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface ICustoTokenCalculator
{
    (decimal CustoEntrada, decimal CustoSaida, decimal CustoTotal) Calcular(
        int tokensEntrada,
        int tokensSaida,
        LlmProvedor provedor);
}
