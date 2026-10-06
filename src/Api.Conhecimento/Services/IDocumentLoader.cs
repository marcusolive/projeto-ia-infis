namespace Api.Conhecimento.Services;

public interface IDocumentLoader
{
    Task<IReadOnlyList<DocumentoCarregado>> CarregarMarkdownAsync(CancellationToken cancellationToken);
}

public sealed record DocumentoCarregado(string NomeArquivo, string Conteudo);
