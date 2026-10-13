using System.Net;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Worker.Atendimento.Configuration;

namespace Worker.Atendimento.Services;

public sealed class MailhogEmailNotificacaoService : IEmailNotificacaoService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<MailhogEmailNotificacaoService> _logger;

    public MailhogEmailNotificacaoService(
        IOptions<SmtpOptions> options,
        ILogger<MailhogEmailNotificacaoService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnviarAtendimentoAsync(
        string usuario,
        string descricao,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.Remetente));
        message.To.Add(MailboxAddress.Parse(_options.Destinatario));
        message.Subject = $"Novo atendimento solicitado por: {usuario}";
        message.Body = new TextPart("html")
        {
            Text = MontarHtml(usuario, descricao),
            ContentTransferEncoding = ContentEncoding.QuotedPrintable
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.None, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation(
            "E-mail enviado via Mailhog. Usuario={Usuario}, Destinatario={Destinatario}",
            usuario,
            _options.Destinatario);
    }

    private static string MontarHtml(string usuario, string descricao)
    {
        var usuarioSafe = WebUtility.HtmlEncode(usuario);
        var descricaoSafe = WebUtility.HtmlEncode(descricao).Replace("\n", "<br/>", StringComparison.Ordinal);

        var sb = new StringBuilder();
        sb.Append("""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8" />
              <title>Novo atendimento</title>
            </head>
            <body style="font-family: Arial, Helvetica, sans-serif; color: #222; background:#f5f5f5; padding:24px;">
              <table width="100%" cellpadding="0" cellspacing="0" style="max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e0e0e0;">
                <tr>
                  <td style="background:#0b3d5c;color:#ffffff;padding:16px 24px;font-size:18px;font-weight:bold;">
                    CorporativoAI — Encaminhamento de atendimento
                  </td>
                </tr>
                <tr>
                  <td style="padding:24px;">
                    <p style="margin:0 0 12px 0;">Um novo atendimento foi encaminhado para suporte humano.</p>
                    <p style="margin:0 0 8px 0;"><strong>Usuário:</strong> 
            """);
        sb.Append(usuarioSafe);
        sb.Append("""
            </p>
                    <p style="margin:16px 0 8px 0;"><strong>Descrição:</strong></p>
                    <div style="padding:12px;background:#f8fafc;border-left:4px solid #0b3d5c;">
            """);
        sb.Append(descricaoSafe);
        sb.Append("""
                    </div>
                    <p style="margin:24px 0 0 0;font-size:12px;color:#666;">
                      Mensagem gerada automaticamente pelo Worker.Atendimento.
                    </p>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """);

        return sb.ToString();
    }
}
