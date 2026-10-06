using System.ComponentModel;
using Api.Conhecimento.Services;
using ModelContextProtocol.Server;

namespace Api.Conhecimento.Tools;

[McpServerToolType]
public sealed class BuscarConhecimentoTool
{
    private readonly IBuscaConhecimentoService _buscaConhecimentoService;

    public BuscarConhecimentoTool(IBuscaConhecimentoService buscaConhecimentoService)
    {
        _buscaConhecimentoService = buscaConhecimentoService;
    }

    [McpServerTool(Name = "BuscarConhecimento"), Description("Busca conhecimento corporativo a partir de uma consulta.")]
    public async Task<string> BuscarConhecimento(
        [Description("Consulta textual para busca de conhecimento.")] string consulta,
        CancellationToken cancellationToken)
    {
        var chunks = await _buscaConhecimentoService.BuscarAsync(consulta, cancellationToken);
        return BuscaConhecimentoService.FormatResultado(chunks);
    }
}
