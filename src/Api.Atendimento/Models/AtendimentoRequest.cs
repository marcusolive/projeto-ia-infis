using System.ComponentModel.DataAnnotations;

namespace Api.Atendimento.Models;

public sealed class AtendimentoRequest
{
    [Required]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    public string Pergunta { get; set; } = string.Empty;
}
