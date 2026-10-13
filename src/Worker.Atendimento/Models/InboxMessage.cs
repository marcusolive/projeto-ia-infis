namespace Worker.Atendimento.Models;

public sealed class InboxMessage
{
    public long Id { get; set; }
    public string IdMensagemFila { get; set; } = string.Empty;
    public DateTimeOffset DataHora { get; set; } = DateTimeOffset.UtcNow;
    public string NomeFila { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string StatusEnvioEmail { get; set; } = string.Empty;
}
