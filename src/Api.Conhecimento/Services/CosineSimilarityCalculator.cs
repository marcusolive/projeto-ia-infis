namespace Api.Conhecimento.Services;

public sealed class CosineSimilarityCalculator : ISimilarityCalculator
{
    public double CalcularSimilaridadeCosseno(ReadOnlySpan<float> vetorA, ReadOnlySpan<float> vetorB)
    {
        if (vetorA.Length == 0 || vetorB.Length == 0)
        {
            throw new ArgumentException("Os vetores de embedding não podem ser vazios.");
        }

        if (vetorA.Length != vetorB.Length)
        {
            throw new ArgumentException(
                $"Os vetores devem ter a mesma dimensão. Recebido {vetorA.Length} e {vetorB.Length}.");
        }

        double produtoEscalar = 0;
        double normaA = 0;
        double normaB = 0;

        for (var i = 0; i < vetorA.Length; i++)
        {
            var a = vetorA[i];
            var b = vetorB[i];
            produtoEscalar += a * b;
            normaA += a * a;
            normaB += b * b;
        }

        if (normaA == 0 || normaB == 0)
        {
            return 0;
        }

        return produtoEscalar / (Math.Sqrt(normaA) * Math.Sqrt(normaB));
    }
}
