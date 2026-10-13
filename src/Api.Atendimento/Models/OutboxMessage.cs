namespace Api.Atendimento.Models;

public sealed class OutboxMessage
{
    public long Id { get; set; }
    public DateTimeOffset DataHoraGravacao { get; set; } = DateTimeOffset.UtcNow;
    public bool Transmitido { get; set; }
    public DateTimeOffset? DataHoraTransmissao { get; set; }
    public string? NomeFila { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}
