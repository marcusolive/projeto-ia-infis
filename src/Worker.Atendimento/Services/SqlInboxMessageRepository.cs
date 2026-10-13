using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Worker.Atendimento.Configuration;
using Worker.Atendimento.Models;

namespace Worker.Atendimento.Services;

public sealed class SqlInboxMessageRepository : IInboxMessageRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlInboxMessageRepository> _logger;
    private static int _schemaGarantido;

    public SqlInboxMessageRepository(
        IOptions<SqlOptions> options,
        ILogger<SqlInboxMessageRepository> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _logger = logger;
    }

    public async Task<long> SalvarAsync(InboxMessage mensagem, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mensagem);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await GarantirSchemaAsync(connection, cancellationToken);

        await using var cmd = new SqlCommand(
            """
            INSERT INTO dbo.InboxMessage
            (
                IdMensagemFila,
                DataHora,
                NomeFila,
                Usuario,
                Descricao,
                StatusEnvioEmail
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @idMensagemFila,
                @dataHora,
                @nomeFila,
                @usuario,
                @descricao,
                @statusEnvioEmail
            );
            """,
            connection);

        cmd.Parameters.AddWithValue("@idMensagemFila", mensagem.IdMensagemFila);
        cmd.Parameters.AddWithValue("@dataHora", mensagem.DataHora);
        cmd.Parameters.AddWithValue("@nomeFila", mensagem.NomeFila);
        cmd.Parameters.AddWithValue("@usuario", mensagem.Usuario);
        cmd.Parameters.AddWithValue("@descricao", mensagem.Descricao);
        cmd.Parameters.AddWithValue("@statusEnvioEmail", mensagem.StatusEnvioEmail);

        var id = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));

        _logger.LogInformation(
            "InboxMessage gravada. Id={Id}, IdMensagemFila={IdMensagemFila}, Status={Status}",
            id,
            mensagem.IdMensagemFila,
            mensagem.StatusEnvioEmail);

        return id;
    }

    private static async Task GarantirSchemaAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _schemaGarantido, 1, 0) == 1)
        {
            return;
        }

        await using var cmd = new SqlCommand(
            """
            IF OBJECT_ID(N'dbo.InboxMessage', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.InboxMessage
                (
                    Id                   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InboxMessage PRIMARY KEY,
                    IdMensagemFila       NVARCHAR(100) NOT NULL,
                    DataHora             DATETIMEOFFSET NOT NULL CONSTRAINT DF_InboxMessage_DataHora DEFAULT (SYSUTCDATETIME()),
                    NomeFila             NVARCHAR(200) NOT NULL,
                    Usuario              NVARCHAR(150) NOT NULL,
                    Descricao            NVARCHAR(MAX) NOT NULL,
                    StatusEnvioEmail     NVARCHAR(50) NOT NULL
                );

                CREATE INDEX IX_InboxMessage_IdMensagemFila
                    ON dbo.InboxMessage (IdMensagemFila);
            END
            """,
            connection);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
