using Api.Atendimento.Configuration;
using Api.Atendimento.Plugins;
using Api.Atendimento.Services;
using Microsoft.OpenApi;
using Microsoft.SemanticKernel;
using MongoDB.Driver;
using OpenAI;

namespace Api.Atendimento;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API Atendimento",
                Version = "v1",
                Description = "Assistente tributário/contábil com RAG (MCP), fallback Gemini, encaminhamento humano (outbox) e custos no SQL Server."
            });
        });
        builder.Services.AddProblemDetails();

        builder.Services
            .AddOptions<McpConhecimentoOptions>()
            .Bind(builder.Configuration.GetSection(McpConhecimentoOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Endpoint),
                "McpConhecimento:Endpoint é obrigatório.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<OpenAIOptions>()
            .Bind(builder.Configuration.GetSection(OpenAIOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
                }
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey),
                "OpenAI:ApiKey (ou variável OPENAI_API_KEY) é obrigatória.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ChatModel),
                "OpenAI:ChatModel é obrigatório.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<GeminiOptions>()
            .Bind(builder.Configuration.GetSection(GeminiOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    options.ApiKey =
                        Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                        ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
                        ?? string.Empty;
                }
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.ChatModel),
                "Gemini:ChatModel é obrigatório.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<MongoHistoricoOptions>()
            .Bind(builder.Configuration.GetSection(MongoHistoricoOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "MongoHistorico:ConnectionString é obrigatória.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName),
                "MongoHistorico:DatabaseName é obrigatório.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.CollectionName),
                "MongoHistorico:CollectionName é obrigatório.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.AgenteId),
                "MongoHistorico:AgenteId é obrigatório.")
            .Validate(options => options.QuantidadeHistorico > 0,
                "MongoHistorico:QuantidadeHistorico deve ser maior que zero.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<CustosTokensOptions>()
            .Bind(builder.Configuration.GetSection(CustosTokensOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "CustosTokens:ConnectionString é obrigatória.")
            .Validate(options => options.PrecoEntradaPorMilhao >= 0,
                "CustosTokens:PrecoEntradaPorMilhao inválido.")
            .Validate(options => options.PrecoSaidaPorMilhao >= 0,
                "CustosTokens:PrecoSaidaPorMilhao inválido.")
            .Validate(options => options.GeminiPrecoEntradaPorMilhao >= 0,
                "CustosTokens:GeminiPrecoEntradaPorMilhao inválido.")
            .Validate(options => options.GeminiPrecoSaidaPorMilhao >= 0,
                "CustosTokens:GeminiPrecoSaidaPorMilhao inválido.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<RabbitMqOptions>()
            .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.HostName),
                "RabbitMq:HostName é obrigatório.")
            .Validate(options => options.Port > 0,
                "RabbitMq:Port inválida.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.QueueName),
                "RabbitMq:QueueName é obrigatório.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<OutboxPublisherOptions>()
            .Bind(builder.Configuration.GetSection(OutboxPublisherOptions.SectionName))
            .Validate(options => options.IntervaloSegundos > 0,
                "OutboxPublisher:IntervaloSegundos deve ser maior que zero.")
            .Validate(options => options.LoteMaximo > 0,
                "OutboxPublisher:LoteMaximo deve ser maior que zero.")
            .ValidateOnStart();

        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAIOptions>>().Value;
            return new OpenAIClient(options.ApiKey);
        });

        builder.Services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoHistoricoOptions>>().Value;
            return new MongoClient(options.ConnectionString);
        });

        builder.Services.AddHttpClient("McpConhecimento");
        builder.Services.AddScoped<IConhecimentoMcpClient, ConhecimentoMcpClient>();
        builder.Services.AddSingleton<IPromptBuilder, PromptBuilder>();
        builder.Services.AddSingleton<ICustoTokenCalculator, CustoTokenCalculator>();
        builder.Services.AddScoped<IOpenAiChatService, OpenAiChatService>();
        builder.Services.AddSingleton<IGeminiChatService, GeminiChatService>();
        builder.Services.AddScoped<IHistoricoConversaRepository, MongoHistoricoConversaRepository>();
        builder.Services.AddScoped<IConsumoTokensRepository, SqlConsumoTokensRepository>();
        builder.Services.AddScoped<IOutboxMessageRepository, SqlOutboxMessageRepository>();
        builder.Services.AddScoped<EncaminharAtendimentoPlugin>();
        builder.Services.AddScoped<IEncaminhamentoAtendimentoService, SemanticKernelEncaminhamentoService>();
        builder.Services.AddScoped<IAtendimentoOrchestrator, AtendimentoOrchestrator>();

        builder.Services.AddScoped(sp =>
        {
            var kernelBuilder = Kernel.CreateBuilder();
            var plugin = sp.GetRequiredService<EncaminharAtendimentoPlugin>();
            kernelBuilder.Plugins.AddFromObject(plugin, SemanticKernelEncaminhamentoService.PluginName);
            return kernelBuilder.Build();
        });

        builder.Services.AddHostedService<OutboxPublisherBackgroundService>();

        var app = builder.Build();

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Atendimento v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
