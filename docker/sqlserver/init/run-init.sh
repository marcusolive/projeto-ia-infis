#!/bin/bash
set -euo pipefail

echo "Aguardando SQL Server para aplicar script de custos..."
sleep 3

/opt/mssql-tools18/bin/sqlcmd \
  -S sqlserver \
  -U sa \
  -P 'Coti@2026' \
  -C \
  -i /docker-init/01-init-custos.sql

echo "Script de inicialização bd-custos-ia aplicado com sucesso."
