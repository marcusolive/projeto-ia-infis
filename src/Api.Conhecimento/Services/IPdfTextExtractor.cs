namespace Api.Conhecimento.Services;

public interface IPdfTextExtractor
{
    Task<string> ExtrairTextoAsync(Stream pdfStream, CancellationToken cancellationToken);
}
