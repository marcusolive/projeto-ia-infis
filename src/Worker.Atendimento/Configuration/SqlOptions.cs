namespace Worker.Atendimento.Configuration;

public sealed class SqlOptions
{
    public const string SectionName = "SqlServer";

    public string ConnectionString { get; set; } = string.Empty;
}
