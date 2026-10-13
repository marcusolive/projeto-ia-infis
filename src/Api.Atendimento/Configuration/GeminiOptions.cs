namespace Api.Atendimento.Configuration;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gemini-3.5-flash-lite";

    /// <summary>
    /// Quando true, usa o endpoint Vertex AI / Express Mode (chaves AQ.*).
    /// Quando null, detecta automaticamente pelo prefixo da chave.
    /// Quando false, usa a Gemini Developer API (chaves AIza*).
    /// </summary>
    public bool? UseVertexAi { get; set; }
}
