using Api.Atendimento.Models;
using Api.Atendimento.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

/// <summary>
/// Mantido por compatibilidade. Prefira POST /atendimentos.
/// </summary>
[ApiController]
[Route("perguntas")]
public class PerguntasController : ControllerBase
{
    private readonly IAtendimentoOrchestrator _orchestrator;
    private readonly ILogger<PerguntasController> _logger;

    public PerguntasController(
        IAtendimentoOrchestrator orchestrator,
        ILogger<PerguntasController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpPost]
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

        _logger.LogInformation("Recebida pergunta via /perguntas (compatibilidade).");

        var resposta = await _orchestrator.ResponderAsync(
            request.Usuario.Trim(),
            request.Pergunta.Trim(),
            cancellationToken);

        return Ok(resposta);
    }
}
