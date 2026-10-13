namespace Api.Atendimento.Configuration;

public sealed class OutboxPublisherOptions
{
    public const string SectionName = "OutboxPublisher";

    public int IntervaloSegundos { get; set; } = 60;
    public int LoteMaximo { get; set; } = 5;
}
