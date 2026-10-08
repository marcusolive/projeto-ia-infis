using Api.Atendimento.Configuration;
using Api.Atendimento.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Services;

public sealed class SqlConsumoTokensRepository : IConsumoTokensRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlConsumoTokensRepository> _logger;

    public SqlConsumoTokensRepository(
        IOptions<CustosTokensOptions> options,
        ILogger<SqlConsumoTokensRepository> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _logger = logger;
    }

    public async Task SalvarAsync(ConsumoTokensRegistro registro, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registro);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand(
            """
            INSERT INTO dbo.consumo_tokens
            (
                agente,
                modelo,
                usuario,
                data_hora,
                tokens_entrada,
                tokens_saida,
                custo_tokens_entrada,
                custo_tokens_saida,
                custo_total,
                id_conversa
            )
            VALUES
            (
                @agente,
                @modelo,
                @usuario,
                @dataHora,
                @tokensEntrada,
                @tokensSaida,
                @custoEntrada,
                @custoSaida,
                @custoTotal,
                @idConversa
            );
            """,
            connection);

        cmd.Parameters.AddWithValue("@agente", registro.Agente);
        cmd.Parameters.AddWithValue("@modelo", registro.Modelo);
        cmd.Parameters.AddWithValue("@usuario", registro.Usuario);
        cmd.Parameters.AddWithValue("@dataHora", registro.DataHora);
        cmd.Parameters.AddWithValue("@tokensEntrada", registro.TokensEntrada);
        cmd.Parameters.AddWithValue("@tokensSaida", registro.TokensSaida);
        cmd.Parameters.AddWithValue("@custoEntrada", registro.CustoTokensEntrada);
        cmd.Parameters.AddWithValue("@custoSaida", registro.CustoTokensSaida);
        cmd.Parameters.AddWithValue("@custoTotal", registro.CustoTotal);
        cmd.Parameters.AddWithValue("@idConversa", registro.IdConversa);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation(
            "Consumo de tokens gravado. Conversa={Conversa}, Total={CustoTotal}",
            registro.IdConversa,
            registro.CustoTotal);
    }
}
