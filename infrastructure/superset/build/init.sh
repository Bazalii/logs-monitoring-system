#!/bin/bash
set -euo pipefail

echo "=== Superset init ==="

# 1) Миграции (идемпотентно)
echo "DB upgrade..."
superset db upgrade

# 2) Создание админа (идемпотентно — если есть, не падаем)
echo "Create admin (if not exists)..."
superset fab create-admin \
  --username admin \
  --firstname Admin \
  --lastname User \
  --email admin@superset.com \
  --password admin \
  || true

# 3) Инициализация
echo "Superset init..."
superset init

echo "=== Superset starting on :8088 ==="
exec superset run -h 0.0.0.0 -p 8088