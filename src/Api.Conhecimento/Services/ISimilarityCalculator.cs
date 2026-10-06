namespace Api.Conhecimento.Services;

public interface ISimilarityCalculator
{
    double CalcularSimilaridadeCosseno(ReadOnlySpan<float> vetorA, ReadOnlySpan<float> vetorB);
}
