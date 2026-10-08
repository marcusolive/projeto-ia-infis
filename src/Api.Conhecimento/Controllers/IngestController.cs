using Api.Conhecimento.Models;
using Api.Conhecimento.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Conhecimento.Controllers;

[ApiController]
[Route("api/v1/ingest")]
[RequestSizeLimit(2 * 1024 * 1024 + 1024)]
public class IngestController : ControllerBase
{
    private readonly IIngestaoDocumentoService _ingestaoDocumentoService;
    private readonly ILogger<IngestController> _logger;

    public IngestController(
        IIngestaoDocumentoService ingestaoDocumentoService,
        ILogger<IngestController> logger)
    {
        _ingestaoDocumentoService = ingestaoDocumentoService;
        _logger = logger;
    }

    /// <summary>
    /// Ingere um PDF: valida, extrai texto, gera chunks/embeddings e grava no banco RAG.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(IngestaoDocumentoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IngestaoDocumentoResponse>> PostAsync(
        [FromForm] IngestaoDocumentoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _ingestaoDocumentoService.IngerirAsync(request, cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new IngestaoDocumentoResponse
                {
                    Mensagem = "Conhecimento cadastrado com sucesso"
                });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Falha de validação na ingestão.");
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Falha de processamento na ingestão.");
            return BadRequest(new { mensagem = ex.Message });
        }
    }
}
