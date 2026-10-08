namespace Api.Conhecimento.Configuration;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public string DocumentosPath { get; set; } = "Documentos";
    public int ChunkSize { get; set; } = 500;
    public int ChunkOverlap { get; set; } = 50;
    public int TopK { get; set; } = 3;
    public string AgenteIdPadrao { get; set; } = "agente-corporativo";
    public long TamanhoMaximoPdfBytes { get; set; } = 2 * 1024 * 1024;
}
