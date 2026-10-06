using ModelContextProtocol.Client;

namespace Api.Atendimento.Services;

public interface IConhecimentoMcpClient
{
    Task<IReadOnlyList<McpClientTool>> ListarToolsAsync(CancellationToken cancellationToken);

    Task<string> BuscarConhecimentoAsync(string consulta, CancellationToken cancellationToken);
}
