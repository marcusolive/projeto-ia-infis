# Outbox, RabbitMQ e Worker de atendimento

Fluxo da etapa 9: encaminhamentos humanos saem da `OutboxMessage`, passam pela fila RabbitMQ `atendimentos` e são consumidos pelo `Worker.Atendimento`, que grava `InboxMessage` e envia e-mail via Mailhog.

## Arquitetura

```text
Api.Atendimento
  → OutboxMessage (SQL, Transmitido=0)
  → OutboxPublisherBackgroundService (a cada 60s, TOP 5)
  → RabbitMQ fila "atendimentos"
       ↓
Worker.Atendimento
  → lê JSON { id, usuario, descricao }
  → envia e-mail (Mailhog)
  → grava InboxMessage (SQL)
```

## Docker

| Serviço | Portas | Credenciais / UI |
|---------|--------|------------------|
| RabbitMQ | `5673` (AMQP), `15673` (management) | user/pass `infis` — http://localhost:15673 |
| Mailhog | `1026` (SMTP), `8026` (UI) | http://localhost:8026 |

```bash
docker compose up -d rabbitmq mailhog
```

## Api.Atendimento (publisher)

- Hosted service: `OutboxPublisherBackgroundService`
- Lê até 5 registros com `Transmitido = 0`
- Publica JSON na fila `atendimentos`
- Atualiza: `Transmitido=1`, `DataHoraTransmissao`, `NomeFila=atendimentos`

Config: `RabbitMq:*`, `OutboxPublisher:*`

## Worker.Atendimento (consumer)

```bash
dotnet run --project src/Worker.Atendimento
```

Para cada mensagem:

1. Envia e-mail HTML  
   - De: `worker-service@infis.com.br`  
   - Para: `atendimento-suporte@infis.com.br`  
   - Assunto: `Novo atendimento solicitado por: {usuario}`
2. Grava `dbo.InboxMessage` com status `Enviado` ou `Falha`

## Tabelas (`bd-custos-ia`)

| Tabela | Papel |
|--------|-------|
| `OutboxMessage` | pendências de publicação |
| `InboxMessage` | histórico de consumo + status do e-mail |

## Validação rápida

1. Gerar encaminhamento humano via `POST /atendimentos`
2. Aguardar até 1 minuto (ou reiniciar a API para processar no startup)
3. Ver mensagem na management UI do RabbitMQ / ou consumo imediato pelo Worker
4. Conferir `OutboxMessage.Transmitido = 1`
5. Conferir linha em `InboxMessage`
6. Abrir Mailhog e validar o e-mail HTML
