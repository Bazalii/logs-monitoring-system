CREATE DATABASE IF NOT EXISTS logly;

CREATE TABLE IF NOT EXISTS logly.logs
(
    id               UUID DEFAULT generateUUIDv7(),
    created_at       DateTime64(3, 'UTC'),
    received_at      DateTime64(3, 'UTC'),

    level            LowCardinality(String),
    source           LowCardinality(String),
    host             LowCardinality(String),
    environment      LowCardinality(String),

    message          String,
    payload          String,

    user_id          Nullable(UInt64) MATERIALIZED nullIf(JSONExtractUInt(payload, 'user_id'), 0),
    duration_ms      Nullable(UInt32) MATERIALIZED nullIf(JSONExtractUInt(payload, 'duration_ms'), 0),
    http_status_code Nullable(UInt16) MATERIALIZED nullIf(toUInt16(JSONExtractUInt(payload, 'http_status_code')), 0),
    error_type       LowCardinality(Nullable(String)) MATERIALIZED nullIf(JSONExtractString(payload, 'error_type'), ''),
    stack_trace      Nullable(String) MATERIALIZED nullIf(JSONExtractString(payload, 'stack_trace'), '')
) ENGINE = MergeTree
      PARTITION BY toDate(created_at)
      ORDER BY (source, level, created_at, id);

-- 1) Общее количество логов по времени
CREATE TABLE IF NOT EXISTS logly.metrics_logs_1m
(
    ts  DateTime('UTC'),
    cnt UInt64
) ENGINE = SummingMergeTree
      PARTITION BY toDate(ts)
      ORDER BY (ts);

-- 2) Распределение логов по уровням
CREATE TABLE IF NOT EXISTS logly.metrics_logs_by_level_1m
(
    ts    DateTime('UTC'),
    level LowCardinality(String),
    cnt   UInt64
) ENGINE = SummingMergeTree
      PARTITION BY toDate(ts)
      ORDER BY (ts, level);

-- 3) Топ-10 источников
CREATE TABLE IF NOT EXISTS logly.metrics_logs_by_source_1m
(
    ts     DateTime('UTC'),
    source LowCardinality(String),
    cnt    UInt64
) ENGINE = SummingMergeTree
      PARTITION BY toDate(ts)
      ORDER BY (ts, source);

-- 4) Топ-3 типов ошибок
CREATE TABLE IF NOT EXISTS logly.metrics_errors_by_type_1m
(
    ts         DateTime('UTC'),
    error_type LowCardinality(String),
    cnt        UInt64
) ENGINE = SummingMergeTree
      PARTITION BY toDate(ts)
      ORDER BY (ts, error_type);

-- 5) Топ-3 ошибок по источникам
CREATE TABLE IF NOT EXISTS logly.metrics_errors_by_source_type_1m
(
    ts         DateTime('UTC'),
    source     LowCardinality(String),
    error_type LowCardinality(String),
    cnt        UInt64
) ENGINE = SummingMergeTree
      PARTITION BY toDate(ts)
      ORDER BY (ts, source, error_type);

-- 6) Ошибки по источникам (для поиска аномального роста на стороне Superset)
CREATE TABLE IF NOT EXISTS logly.metrics_errors_by_source_1m
(
    ts     DateTime('UTC'),
    source LowCardinality(String),
    cnt    UInt64
) ENGINE = SummingMergeTree
      PARTITION BY toDate(ts)
      ORDER BY (ts, source);

-- 7) Dead service detection
CREATE TABLE IF NOT EXISTS logly.service_last_seen
(
    source      LowCardinality(String),
    environment LowCardinality(String),
    last_seen   DateTime64(3, 'UTC')
) ENGINE = ReplacingMergeTree(last_seen)
      ORDER BY (source, environment);