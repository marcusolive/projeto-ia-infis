namespace Worker.Atendimento.Configuration;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1026;
    public string Remetente { get; set; } = "worker-service@infis.com.br";
    public string Destinatario { get; set; } = "atendimento-suporte@infis.com.br";
}
