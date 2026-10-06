using Microsoft.AspNetCore.Mvc;

namespace Api.Conhecimento.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Health check da API Conhecimento.");
        return Task.FromResult<IActionResult>(Ok(new { status = "Healthy", service = "Api.Conhecimento" }));
    }
}
