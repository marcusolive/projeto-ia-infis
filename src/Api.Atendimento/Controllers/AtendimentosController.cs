using Api.Atendimento.Models;
using Api.Atendimento.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

[ApiController]
[Route("atendimentos")]
public class AtendimentosController : ControllerBase
{
    private readonly IAtendimentoOrchestrator _orchestrator;
    private readonly ILogger<AtendimentosController> _logger;

    public AtendimentosController(
        IAtendimentoOrchestrator orchestrator,
        ILogger<AtendimentosController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Recebe um novo atendimento (usuário + pergunta), busca contexto via MCP/RAG e responde com OpenAI.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AtendimentoResposta), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AtendimentoResposta>> PostAsync(
        [FromBody] AtendimentoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario))
        {
            return BadRequest(new { erro = "Nome do usuário é obrigatório." });
        }

        if (string.IsNullOrWhiteSpace(request.Pergunta))
        {
            return BadRequest(new { erro = "Pergunta é obrigatória." });
        }

        _logger.LogInformation("Novo atendimento recebido para o usuário {Usuario}.", request.Usuario);

        var resposta = await _orchestrator.ResponderAsync(
            request.Usuario.Trim(),
            request.Pergunta.Trim(),
            cancellationToken);

        return Ok(resposta);
    }
}
