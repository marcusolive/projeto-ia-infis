using Microsoft.SemanticKernel;

namespace Api.Atendimento.Services;

public sealed class SemanticKernelEncaminhamentoService : IEncaminhamentoAtendimentoService
{
    public const string PluginName = "EncaminharAtendimento";
    public const string FunctionName = "encaminhar_atendimento_humano";

    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelEncaminhamentoService> _logger;

    public SemanticKernelEncaminhamentoService(
        Kernel kernel,
        ILogger<SemanticKernelEncaminhamentoService> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task EncaminharAsync(string usuario, string descricao, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        var argumentos = new KernelArguments
        {
            ["usuario"] = usuario,
            ["descricao"] = descricao
        };

        var resultado = await _kernel.InvokeAsync(
            PluginName,
            FunctionName,
            argumentos,
            cancellationToken);

        _logger.LogInformation(
            "Função Semantic Kernel de encaminhamento executada. Usuario={Usuario}. Resultado={Resultado}",
            usuario,
            resultado.GetValue<string>());
    }
}
