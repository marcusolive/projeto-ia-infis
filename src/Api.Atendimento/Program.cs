using Api.Atendimento.Configuration;
using Api.Atendimento.Services;
using Microsoft.OpenApi;
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
                Description = "API orquestradora de atendimento corporativo com cliente MCP e OpenAI."
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

        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAIOptions>>().Value;
            return new OpenAIClient(options.ApiKey);
        });

        builder.Services.AddHttpClient("McpConhecimento");
        builder.Services.AddScoped<IConhecimentoMcpClient, ConhecimentoMcpClient>();
        builder.Services.AddSingleton<IPromptBuilder, PromptBuilder>();
        builder.Services.AddScoped<IOpenAiChatService, OpenAiChatService>();
        builder.Services.AddScoped<IAtendimentoOrchestrator, AtendimentoOrchestrator>();

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
