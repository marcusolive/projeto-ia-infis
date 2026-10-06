using Api.Conhecimento.Services;

namespace Api.Conhecimento.Tests;

public class CosineSimilarityCalculatorTests
{
    private readonly CosineSimilarityCalculator _sut = new();

    [Fact]
    public void CalcularSimilaridadeCosseno_VetoresIdenticos_RetornaUm()
    {
        float[] vetor = [1f, 2f, 3f];

        var similaridade = _sut.CalcularSimilaridadeCosseno(vetor, vetor);

        Assert.Equal(1d, similaridade, precision: 6);
    }

    [Fact]
    public void CalcularSimilaridadeCosseno_VetoresOrtogonais_RetornaZero()
    {
        float[] vetorA = [1f, 0f];
        float[] vetorB = [0f, 1f];

        var similaridade = _sut.CalcularSimilaridadeCosseno(vetorA, vetorB);

        Assert.Equal(0d, similaridade, precision: 6);
    }

    [Fact]
    public void CalcularSimilaridadeCosseno_VetoresOpostos_RetornaMenosUm()
    {
        float[] vetorA = [1f, 1f];
        float[] vetorB = [-1f, -1f];

        var similaridade = _sut.CalcularSimilaridadeCosseno(vetorA, vetorB);

        Assert.Equal(-1d, similaridade, precision: 6);
    }

    [Fact]
    public void CalcularSimilaridadeCosseno_VetoresProporcionais_RetornaUm()
    {
        float[] vetorA = [1f, 2f, 3f];
        float[] vetorB = [2f, 4f, 6f];

        var similaridade = _sut.CalcularSimilaridadeCosseno(vetorA, vetorB);

        Assert.Equal(1d, similaridade, precision: 6);
    }

    [Fact]
    public void CalcularSimilaridadeCosseno_DimensoesDiferentes_LancaArgumentException()
    {
        float[] vetorA = [1f, 2f];
        float[] vetorB = [1f, 2f, 3f];

        Assert.Throws<ArgumentException>(() =>
            _sut.CalcularSimilaridadeCosseno(vetorA, vetorB));
    }

    [Fact]
    public void CalcularSimilaridadeCosseno_VetorVazio_LancaArgumentException()
    {
        float[] vetorA = [];
        float[] vetorB = [1f];

        Assert.Throws<ArgumentException>(() =>
            _sut.CalcularSimilaridadeCosseno(vetorA, vetorB));
    }

    [Fact]
    public void CalcularSimilaridadeCosseno_NormaZero_RetornaZero()
    {
        float[] vetorA = [0f, 0f];
        float[] vetorB = [1f, 2f];

        var similaridade = _sut.CalcularSimilaridadeCosseno(vetorA, vetorB);

        Assert.Equal(0d, similaridade, precision: 6);
    }
}
