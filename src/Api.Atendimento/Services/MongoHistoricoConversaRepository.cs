using Api.Atendimento.Configuration;
using Api.Atendimento.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Api.Atendimento.Services;

public sealed class MongoHistoricoConversaRepository : IHistoricoConversaRepository
{
    private readonly IMongoCollection<HistoricoConversaDocumento> _collection;
    private readonly ILogger<MongoHistoricoConversaRepository> _logger;

    public MongoHistoricoConversaRepository(
        IMongoClient mongoClient,
        IOptions<MongoHistoricoOptions> options,
        ILogger<MongoHistoricoConversaRepository> logger)
    {
        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<HistoricoConversaDocumento>(config.CollectionName);
        _logger = logger;
    }

    public async Task<IReadOnlyList<HistoricoConversaDocumento>> ObterUltimasPorUsuarioAsync(
        string usuario,
        int quantidade,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario);

        if (quantidade <= 0)
        {
            return Array.Empty<HistoricoConversaDocumento>();
        }

        var filtro = Builders<HistoricoConversaDocumento>.Filter.Eq(x => x.Usuario, usuario);

        var historico = await _collection
            .Find(filtro)
            .SortByDescending(x => x.DataHora)
            .Limit(quantidade)
            .ToListAsync(cancellationToken);

        // Ordem cronológica (mais antiga → mais recente) para o prompt
        historico.Reverse();

        _logger.LogInformation(
            "Recuperadas {Quantidade} conversas anteriores do usuário {Usuario}.",
            historico.Count,
            usuario);

        return historico;
    }

    public async Task SalvarAsync(HistoricoConversaDocumento documento, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(documento);

        await _collection.InsertOneAsync(documento, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Histórico gravado. Id={Id}, Usuario={Usuario}, Agente={Agente}",
            documento.Id,
            documento.Usuario,
            documento.Agente);
    }
}
