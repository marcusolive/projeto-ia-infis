using Api.Conhecimento.Models;

namespace Api.Conhecimento.Services;

public interface IIngestaoDocumentoService
{
    Task IngerirAsync(IngestaoDocumentoRequest request, CancellationToken cancellationToken);
}
