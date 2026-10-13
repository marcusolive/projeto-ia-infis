using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Worker.Atendimento.Configuration;
using Worker.Atendimento.Models;

namespace Worker.Atendimento.Services;

public sealed class AtendimentoQueueConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly ILogger<AtendimentoQueueConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public AtendimentoQueueConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> rabbitOptions,
        ILogger<AtendimentoQueueConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbitOptions = rabbitOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Consumidor RabbitMQ iniciado. Fila={Fila}, Host={Host}:{Port}",
            _rabbitOptions.QueueName,
            _rabbitOptions.HostName,
            _rabbitOptions.Port);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConectarEConsumirAsync(stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no consumidor RabbitMQ. Reconectando em 5s...");
                await DisposeAmqpAsync();
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        await DisposeAmqpAsync();
    }

    private async Task ConectarEConsumirAsync(CancellationToken cancellationToken)
    {
        await DisposeAmqpAsync();

        var factory = new ConnectionFactory
        {
            HostName = _rabbitOptions.HostName,
            Port = _rabbitOptions.Port,
            UserName = _rabbitOptions.UserName,
            Password = _rabbitOptions.Password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: _rabbitOptions.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(0, 1, false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: _rabbitOptions.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Aguardando mensagens na fila {Fila}.", _rabbitOptions.QueueName);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var channel = _channel
            ?? throw new InvalidOperationException("Canal RabbitMQ não inicializado.");

        var bodyJson = Encoding.UTF8.GetString(ea.Body.ToArray());
        var idMensagemFila = ea.BasicProperties.MessageId
            ?? ea.DeliveryTag.ToString();

        try
        {
            var mensagem = JsonSerializer.Deserialize<AtendimentoFilaMensagem>(bodyJson, JsonOptions)
                ?? throw new InvalidOperationException("Payload da fila inválido.");

            if (string.IsNullOrWhiteSpace(mensagem.Usuario) || string.IsNullOrWhiteSpace(mensagem.Descricao))
            {
                throw new InvalidOperationException("Payload da fila sem usuario/descricao.");
            }

            idMensagemFila = mensagem.Id.ToString();

            using var scope = _scopeFactory.CreateScope();
            var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxMessageRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailNotificacaoService>();

            string statusEmail;
            try
            {
                await emailService.EnviarAtendimentoAsync(
                    mensagem.Usuario,
                    mensagem.Descricao,
                    CancellationToken.None);
                statusEmail = "Enviado";
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Falha ao enviar e-mail do atendimento Id={Id}.", mensagem.Id);
                statusEmail = "Falha";
            }

            await inboxRepository.SalvarAsync(
                new InboxMessage
                {
                    IdMensagemFila = idMensagemFila,
                    DataHora = DateTimeOffset.UtcNow,
                    NomeFila = _rabbitOptions.QueueName,
                    Usuario = mensagem.Usuario,
                    Descricao = mensagem.Descricao,
                    StatusEnvioEmail = statusEmail
                },
                CancellationToken.None);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            _logger.LogInformation(
                "Mensagem processada. OutboxId={Id}, StatusEmail={Status}",
                mensagem.Id,
                statusEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar mensagem da fila. Payload={Payload}", bodyJson);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task DisposeAmqpAsync()
    {
        if (_channel is not null)
        {
            try
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }
            catch
            {
                // ignore
            }

            _channel = null;
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
            catch
            {
                // ignore
            }

            _connection = null;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeAmqpAsync();
        await base.StopAsync(cancellationToken);
    }
}
