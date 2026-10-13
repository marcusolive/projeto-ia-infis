using System.Text;
using System.Text.Json;
using Api.Atendimento.Configuration;
using Api.Atendimento.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Api.Atendimento.Services;

public sealed class OutboxPublisherBackgroundService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly OutboxPublisherOptions _publisherOptions;
    private readonly ILogger<OutboxPublisherBackgroundService> _logger;

    public OutboxPublisherBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<OutboxPublisherOptions> publisherOptions,
        ILogger<OutboxPublisherBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbitOptions = rabbitOptions.Value;
        _publisherOptions = publisherOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = TimeSpan.FromSeconds(Math.Max(1, _publisherOptions.IntervaloSegundos));
        _logger.LogInformation(
            "OutboxPublisher iniciado. Intervalo={Intervalo}s, Lote={Lote}, Fila={Fila}",
            intervalo.TotalSeconds,
            _publisherOptions.LoteMaximo,
            _rabbitOptions.QueueName);

        using var timer = new PeriodicTimer(intervalo);

        // Processa uma vez no startup e depois a cada intervalo.
        await ProcessarLoteAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessarLoteAsync(stoppingToken);
        }
    }

    private async Task ProcessarLoteAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();

            var pendentes = await repository.ObterPendentesAsync(
                _publisherOptions.LoteMaximo,
                cancellationToken);

            if (pendentes.Count == 0)
            {
                _logger.LogDebug("Nenhuma OutboxMessage pendente para transmitir.");
                return;
            }

            var factory = new ConnectionFactory
            {
                HostName = _rabbitOptions.HostName,
                Port = _rabbitOptions.Port,
                UserName = _rabbitOptions.UserName,
                Password = _rabbitOptions.Password
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: _rabbitOptions.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            foreach (var mensagem in pendentes)
            {
                var payload = new AtendimentoFilaMensagem
                {
                    Id = mensagem.Id,
                    Usuario = mensagem.Usuario,
                    Descricao = mensagem.Descricao
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions));

                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: _rabbitOptions.QueueName,
                    body: body,
                    cancellationToken: cancellationToken);

                await repository.MarcarComoTransmitidoAsync(
                    mensagem.Id,
                    _rabbitOptions.QueueName,
                    cancellationToken);

                _logger.LogInformation(
                    "OutboxMessage {Id} publicada na fila {Fila}.",
                    mensagem.Id,
                    _rabbitOptions.QueueName);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar lote da OutboxMessage.");
        }
    }
}
