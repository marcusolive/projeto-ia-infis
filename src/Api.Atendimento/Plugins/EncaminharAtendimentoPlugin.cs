using System.ComponentModel;
using Api.Atendimento.Models;
using Api.Atendimento.Services;
using Microsoft.SemanticKernel;

namespace Api.Atendimento.Plugins;

public sealed class EncaminharAtendimentoPlugin
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;

    public EncaminharAtendimentoPlugin(IOutboxMessageRepository outboxMessageRepository)
    {
        _outboxMessageRepository = outboxMessageRepository;
    }

    [KernelFunction("encaminhar_atendimento_humano")]
    [Description(
        "Encaminha o atendimento para suporte humano gravando um registro OutboxMessage no SQL Server.")]
    public async Task<string> EncaminharAsync(
        [Description("Nome do usuário do atendimento")] string usuario,
        [Description("Resumo do atendimento gerado pela IA para o suporte humano")] string descricao,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        var id = await _outboxMessageRepository.SalvarAsync(
            new OutboxMessage
            {
                DataHoraGravacao = DateTimeOffset.UtcNow,
                Transmitido = false,
                DataHoraTransmissao = null,
                NomeFila = null,
                Usuario = usuario.Trim(),
                Descricao = descricao.Trim()
            },
            cancellationToken);

        return $"Encaminhamento registrado na OutboxMessage com Id={id}.";
    }
}
