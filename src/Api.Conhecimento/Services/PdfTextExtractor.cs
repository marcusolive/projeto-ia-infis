using System.Text;
using UglyToad.PdfPig;

namespace Api.Conhecimento.Services;

public sealed class PdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtrairTextoAsync(Stream pdfStream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);
        cancellationToken.ThrowIfCancellationRequested();

        using var document = PdfDocument.Open(pdfStream);
        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var texto = page.Text;
            if (!string.IsNullOrWhiteSpace(texto))
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }

                builder.Append(texto.Trim());
            }
        }

        var resultado = builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(resultado))
        {
            throw new InvalidOperationException("Não foi possível extrair texto do PDF informado.");
        }

        return Task.FromResult(resultado);
    }
}
