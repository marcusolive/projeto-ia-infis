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
