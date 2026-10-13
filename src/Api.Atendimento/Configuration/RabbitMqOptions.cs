namespace Api.Atendimento.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5673;
    public string UserName { get; set; } = "infis";
    public string Password { get; set; } = "infis";
    public string QueueName { get; set; } = "atendimentos";
}
