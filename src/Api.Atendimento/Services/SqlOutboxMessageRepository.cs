using Api.Atendimento.Configuration;
using Api.Atendimento.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Services;

public sealed class SqlOutboxMessageRepository : IOutboxMessageRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlOutboxMessageRepository> _logger;
    private static int _schemaGarantido;

    public SqlOutboxMessageRepository(
        IOptions<CustosTokensOptions> options,
        ILogger<SqlOutboxMessageRepository> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _logger = logger;
    }

    public async Task<long> SalvarAsync(OutboxMessage mensagem, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mensagem);
        ArgumentException.ThrowIfNullOrWhiteSpace(mensagem.Usuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(mensagem.Descricao);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await GarantirSchemaAsync(connection, cancellationToken);

        await using var cmd = new SqlCommand(
            """
            INSERT INTO dbo.OutboxMessage
            (
                DataHoraGravacao,
                Transmitido,
                DataHoraTransmissao,
                NomeFila,
                Usuario,
                Descricao
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @dataHoraGravacao,
                @transmitido,
                @dataHoraTransmissao,
                @nomeFila,
                @usuario,
                @descricao
            );
            """,
            connection);

        cmd.Parameters.AddWithValue("@dataHoraGravacao", mensagem.DataHoraGravacao);
        cmd.Parameters.AddWithValue("@transmitido", mensagem.Transmitido);
        cmd.Parameters.AddWithValue("@dataHoraTransmissao", (object?)mensagem.DataHoraTransmissao ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@nomeFila", (object?)mensagem.NomeFila ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@usuario", mensagem.Usuario);
        cmd.Parameters.AddWithValue("@descricao", mensagem.Descricao);

        var id = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));

        _logger.LogInformation(
            "OutboxMessage gravada. Id={Id}, Usuario={Usuario}",
            id,
            mensagem.Usuario);

        return id;
    }

    public async Task<IReadOnlyList<OutboxMessage>> ObterPendentesAsync(
        int limite,
        CancellationToken cancellationToken)
    {
        if (limite <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limite));
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await GarantirSchemaAsync(connection, cancellationToken);

        await using var cmd = new SqlCommand(
            """
            SELECT TOP (@limite)
                Id,
                DataHoraGravacao,
                Transmitido,
                DataHoraTransmissao,
                NomeFila,
                Usuario,
                Descricao
            FROM dbo.OutboxMessage
            WHERE Transmitido = 0
            ORDER BY Id ASC;
            """,
            connection);

        cmd.Parameters.AddWithValue("@limite", limite);

        var lista = new List<OutboxMessage>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(new OutboxMessage
            {
                Id = reader.GetInt64(0),
                DataHoraGravacao = reader.GetFieldValue<DateTimeOffset>(1),
                Transmitido = reader.GetBoolean(2),
                DataHoraTransmissao = reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3),
                NomeFila = reader.IsDBNull(4) ? null : reader.GetString(4),
                Usuario = reader.GetString(5),
                Descricao = reader.GetString(6)
            });
        }

        return lista;
    }

    public async Task MarcarComoTransmitidoAsync(
        long id,
        string nomeFila,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeFila);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await GarantirSchemaAsync(connection, cancellationToken);

        await using var cmd = new SqlCommand(
            """
            UPDATE dbo.OutboxMessage
            SET
                Transmitido = 1,
                DataHoraTransmissao = @dataHoraTransmissao,
                NomeFila = @nomeFila
            WHERE Id = @id;
            """,
            connection);

        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@dataHoraTransmissao", DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue("@nomeFila", nomeFila);

        var afetados = await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation(
            "OutboxMessage marcada como transmitida. Id={Id}, Fila={Fila}, Linhas={Linhas}",
            id,
            nomeFila,
            afetados);
    }

    private static async Task GarantirSchemaAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _schemaGarantido, 1, 0) == 1)
        {
            return;
        }

        await using var cmd = new SqlCommand(
            """
            IF OBJECT_ID(N'dbo.OutboxMessage', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.OutboxMessage
                (
                    Id                   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OutboxMessage PRIMARY KEY,
                    DataHoraGravacao     DATETIMEOFFSET NOT NULL CONSTRAINT DF_OutboxMessage_DataHoraGravacao DEFAULT (SYSUTCDATETIME()),
                    Transmitido          BIT NOT NULL CONSTRAINT DF_OutboxMessage_Transmitido DEFAULT (0),
                    DataHoraTransmissao  DATETIMEOFFSET NULL,
                    NomeFila             NVARCHAR(200) NULL,
                    Usuario              NVARCHAR(150) NOT NULL,
                    Descricao            NVARCHAR(MAX) NOT NULL
                );

                CREATE INDEX IX_OutboxMessage_Transmitido_Data
                    ON dbo.OutboxMessage (Transmitido, DataHoraGravacao);
            END
            """,
            connection);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
