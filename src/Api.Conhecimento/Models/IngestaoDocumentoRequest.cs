using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Api.Conhecimento.Models;

public sealed class IngestaoDocumentoRequest
{
    [Required]
    [FromForm(Name = "usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    [FromForm(Name = "codigoDocumento")]
    public string CodigoDocumento { get; set; } = string.Empty;

    [Required]
    [FromForm(Name = "validade")]
    public DateTimeOffset Validade { get; set; }

    [Required]
    [FromForm(Name = "arquivo")]
    public IFormFile Arquivo { get; set; } = null!;
}
