IF DB_ID(N'bd-custos-ia') IS NULL
BEGIN
    CREATE DATABASE [bd-custos-ia];
END
GO

USE [bd-custos-ia];
GO

IF OBJECT_ID(N'dbo.consumo_tokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.consumo_tokens
    (
        id                      BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_consumo_tokens PRIMARY KEY,
        agente                  NVARCHAR(150) NOT NULL,
        modelo                  NVARCHAR(100) NOT NULL,
        usuario                 NVARCHAR(150) NOT NULL,
        data_hora               DATETIMEOFFSET NOT NULL CONSTRAINT DF_consumo_tokens_data_hora DEFAULT (SYSUTCDATETIME()),
        tokens_entrada          INT NOT NULL,
        tokens_saida            INT NOT NULL,
        custo_tokens_entrada    DECIMAL(18, 8) NOT NULL,
        custo_tokens_saida      DECIMAL(18, 8) NOT NULL,
        custo_total             DECIMAL(18, 8) NOT NULL,
        id_conversa             NVARCHAR(50) NOT NULL
    );

    CREATE INDEX IX_consumo_tokens_usuario_data
        ON dbo.consumo_tokens (usuario, data_hora DESC);

    CREATE INDEX IX_consumo_tokens_id_conversa
        ON dbo.consumo_tokens (id_conversa);
END
GO

IF OBJECT_ID(N'dbo.OutboxMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboxMessage
    (
        Id                   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OutboxMessage PRIMARY KEY,
        DataHoraGravacao     DATETIMEOFFSET NOT NULL CONSTRAINT DF_OutboxMessage_DataHoraGravacao DEFAULT (SYSUTCDATETIME()),
        Transmitido          BIT NOT NULL CONSTRAINT DF_OutboxMessage_Transmitido DEFAULT (0),
        DataHoraTransmissao  DATETIMEOFFSET NULL,
        NomeFila             NVARCHAR(200) NULL,
        Usuario              NVARCHAR(150) NOT NULL,
        Descricao            NVARCHAR(MAX) NOT NULL
    );

    CREATE INDEX IX_OutboxMessage_Transmitido_Data
        ON dbo.OutboxMessage (Transmitido, DataHoraGravacao);
END
GO

IF OBJECT_ID(N'dbo.InboxMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InboxMessage
    (
        Id                   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InboxMessage PRIMARY KEY,
        IdMensagemFila       NVARCHAR(100) NOT NULL,
        DataHora             DATETIMEOFFSET NOT NULL CONSTRAINT DF_InboxMessage_DataHora DEFAULT (SYSUTCDATETIME()),
        NomeFila             NVARCHAR(200) NOT NULL,
        Usuario              NVARCHAR(150) NOT NULL,
        Descricao            NVARCHAR(MAX) NOT NULL,
        StatusEnvioEmail     NVARCHAR(50) NOT NULL
    );

    CREATE INDEX IX_InboxMessage_IdMensagemFila
        ON dbo.InboxMessage (IdMensagemFila);
END
GO
