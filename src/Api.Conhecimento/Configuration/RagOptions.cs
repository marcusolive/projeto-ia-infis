namespace Api.Conhecimento.Configuration;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public string DocumentosPath { get; set; } = "Documentos";
    public int ChunkSize { get; set; } = 500;
    public int ChunkOverlap { get; set; } = 50;
    public int TopK { get; set; } = 3;
}
