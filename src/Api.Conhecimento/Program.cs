using Api.Conhecimento.Configuration;
using Api.Conhecimento.Services;
using Api.Conhecimento.Tools;
using Microsoft.OpenApi;
using Npgsql;
using OpenAI;
using Pgvector.Npgsql;

namespace Api.Conhecimento;

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
                Title = "API Conhecimento",
                Version = "v1",
                Description = "API de conhecimento corporativo com servidor MCP em /mcp e ingestão PDF."
            });
        });
        builder.Services.AddProblemDetails();

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
            .Validate(options => !string.IsNullOrWhiteSpace(options.EmbeddingModel),
                "OpenAI:EmbeddingModel é obrigatório.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<RagOptions>()
            .Bind(builder.Configuration.GetSection(RagOptions.SectionName))
            .Validate(options => options.ChunkSize > 0, "Rag:ChunkSize deve ser maior que zero.")
            .Validate(options => options.ChunkOverlap >= 0, "Rag:ChunkOverlap não pode ser negativo.")
            .Validate(options => options.TopK > 0, "Rag:TopK deve ser maior que zero.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.AgenteIdPadrao),
                "Rag:AgenteIdPadrao é obrigatório.")
            .Validate(options => options.TamanhoMaximoPdfBytes > 0,
                "Rag:TamanhoMaximoPdfBytes deve ser maior que zero.")
            .ValidateOnStart();

        var connectionString = builder.Configuration.GetConnectionString("Rag");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Rag é obrigatória.");
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        builder.Services.AddSingleton(dataSourceBuilder.Build());

        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAIOptions>>().Value;
            return new OpenAIClient(options.ApiKey);
        });

        builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        builder.Services.AddSingleton<ISimilarityCalculator, CosineSimilarityCalculator>();
        builder.Services.AddSingleton<IChunkingService, ChunkingService>();
        builder.Services.AddScoped<IDocumentLoader, DocumentLoader>();
        builder.Services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
        builder.Services.AddScoped<IBuscaConhecimentoService, BuscaConhecimentoService>();
        builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
        builder.Services.AddScoped<IConhecimentoChunkRepository, PostgresConhecimentoChunkRepository>();
        builder.Services.AddScoped<IIngestaoDocumentoService, IngestaoDocumentoService>();
        builder.Services.AddHostedService<KnowledgeBaseInitializer>();

        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<BuscarConhecimentoTool>();

        var app = builder.Build();

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Conhecimento v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.MapMcp("/mcp");
        app.Run();
    }
}
