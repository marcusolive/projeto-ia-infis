using Worker.Atendimento.Configuration;
using Worker.Atendimento.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.HostName), "RabbitMq:HostName é obrigatório.")
    .Validate(options => options.Port > 0, "RabbitMq:Port inválida.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.QueueName), "RabbitMq:QueueName é obrigatório.")
    .ValidateOnStart();

builder.Services
    .AddOptions<SqlOptions>()
    .Bind(builder.Configuration.GetSection(SqlOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        "SqlServer:ConnectionString é obrigatória.")
    .ValidateOnStart();

builder.Services
    .AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "Smtp:Host é obrigatório.")
    .Validate(options => options.Port > 0, "Smtp:Port inválida.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Remetente), "Smtp:Remetente é obrigatório.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Destinatario), "Smtp:Destinatario é obrigatório.")
    .ValidateOnStart();

builder.Services.AddSingleton<IInboxMessageRepository, SqlInboxMessageRepository>();
builder.Services.AddSingleton<IEmailNotificacaoService, MailhogEmailNotificacaoService>();
builder.Services.AddHostedService<AtendimentoQueueConsumer>();

var host = builder.Build();
host.Run();
