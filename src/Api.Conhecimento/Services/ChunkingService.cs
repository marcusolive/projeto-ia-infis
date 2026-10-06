using Api.Conhecimento.Configuration;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Services;

public sealed class ChunkingService : IChunkingService
{
    private readonly RagOptions _options;

    public ChunkingService(IOptions<RagOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<TextoChunk> Dividir(string fonte, string conteudo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fonte);
        ArgumentException.ThrowIfNullOrWhiteSpace(conteudo);

        var tamanho = Math.Max(1, _options.ChunkSize);
        var overlap = Math.Clamp(_options.ChunkOverlap, 0, tamanho - 1);
        var texto = conteudo.Replace("\r\n", "\n").Trim();

        if (texto.Length <= tamanho)
        {
            return [new TextoChunk(fonte, texto, 0)];
        }

        var chunks = new List<TextoChunk>();
        var inicio = 0;
        var indice = 0;

        while (inicio < texto.Length)
        {
            var comprimento = Math.Min(tamanho, texto.Length - inicio);
            var fim = inicio + comprimento;

            if (fim < texto.Length)
            {
                var limiteQuebra = EncontrarPontoDeQuebra(texto, inicio, fim);
                if (limiteQuebra > inicio)
                {
                    fim = limiteQuebra;
                }
            }

            var trecho = texto[inicio..fim].Trim();
            if (!string.IsNullOrWhiteSpace(trecho))
            {
                chunks.Add(new TextoChunk(fonte, trecho, indice++));
            }

            if (fim >= texto.Length)
            {
                break;
            }

            inicio = Math.Max(fim - overlap, inicio + 1);
        }

        return chunks;
    }

    private static int EncontrarPontoDeQuebra(string texto, int inicio, int fim)
    {
        for (var i = fim; i > inicio; i--)
        {
            var c = texto[i - 1];
            if (c is '\n' or '.' or ';' or '!')
            {
                return i;
            }
        }

        for (var i = fim; i > inicio; i--)
        {
            if (char.IsWhiteSpace(texto[i - 1]))
            {
                return i;
            }
        }

        return fim;
    }
}
