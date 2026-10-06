using Api.Atendimento.Models;
using Api.Atendimento.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

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
        [FromBody] PerguntaRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Pergunta))
        {
            return BadRequest(new { erro = "Pergunta é obrigatória." });
        }

        _logger.LogInformation("Recebida pergunta via REST.");

        var resposta = await _orchestrator.ResponderAsync(request.Pergunta, cancellationToken);
        return Ok(resposta);
    }
}
