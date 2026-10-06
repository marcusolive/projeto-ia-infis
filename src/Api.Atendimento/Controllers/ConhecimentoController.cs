using Api.Atendimento.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

[ApiController]
[Route("conhecimento")]
public class ConhecimentoController : ControllerBase
{
    private readonly IConhecimentoMcpClient _conhecimentoMcpClient;
    private readonly ILogger<ConhecimentoController> _logger;

    public ConhecimentoController(
        IConhecimentoMcpClient conhecimentoMcpClient,
        ILogger<ConhecimentoController> logger)
    {
        _conhecimentoMcpClient = conhecimentoMcpClient;
        _logger = logger;
    }

    [HttpGet("tools")]
    public async Task<IActionResult> ListarToolsAsync(CancellationToken cancellationToken)
    {
        var tools = await _conhecimentoMcpClient.ListarToolsAsync(cancellationToken);

        var resposta = tools.Select(tool => new
        {
            tool.Name,
            tool.Description
        });

        return Ok(resposta);
    }

    [HttpPost("buscar")]
    public async Task<IActionResult> BuscarAsync(
        [FromBody] BuscarConhecimentoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Consulta))
        {
            return BadRequest(new { erro = "Consulta é obrigatória." });
        }

        _logger.LogInformation("Solicitando busca MCP para consulta: {Consulta}", request.Consulta);

        var resultado = await _conhecimentoMcpClient.BuscarConhecimentoAsync(
            request.Consulta,
            cancellationToken);

        return Ok(new
        {
            tool = "BuscarConhecimento",
            consulta = request.Consulta,
            resultado
        });
    }
}

public sealed record BuscarConhecimentoRequest(string Consulta);
