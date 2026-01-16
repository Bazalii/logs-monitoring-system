from __future__ import annotations

import json
from datetime import timedelta

import pendulum
import requests
from airflow import DAG
from airflow.models import Connection
from airflow.operators.python import PythonOperator
from airflow.utils.dates import days_ago


CLICKHOUSE_CONN_ID = "clickhouse_default"

def _get_clickhouse_http_base() -> tuple[str, str, str, str]:
    conn: Connection = Connection.get_connection_from_secrets(CLICKHOUSE_CONN_ID)

    host = conn.host or "clickhouse"
    port = conn.port or 8123
    base_url = f"http://{host}:{port}"

    user = conn.login or ""
    password = conn.password or ""

    extra = conn.extra_dejson or {}
    database = extra.get("database", "logly")

    return base_url, user, password, database


def ch_execute(sql: str) -> None:
    base_url, user, password, _db = _get_clickhouse_http_base()

    params = {"query": sql}
    auth = (user, password) if user or password else None

    resp = requests.post(f"{base_url}/", params=params, auth=auth, timeout=60)
    if resp.status_code >= 300:
        raise RuntimeError(
            f"ClickHouse query failed: {resp.status_code} {resp.text}\nSQL:\n{sql}"
        )


# ---------- SQL builders ----------

def _window_sql(data_interval_start, data_interval_end) -> tuple[str, str]:
    # ClickHouse expects UTC DateTime strings.
    start = data_interval_start.in_timezone("UTC").strftime("%Y-%m-%d %H:%M:%S")
    end = data_interval_end.in_timezone("UTC").strftime("%Y-%m-%d %H:%M:%S")
    return start, end


def load_metrics_for_window(**context) -> None:
    """
    Builds metrics for the previous minute:
    [data_interval_start, data_interval_end)
    """
    di_start = context["data_interval_start"]
    di_end = context["data_interval_end"]
    window_start, window_end = _window_sql(di_start, di_end)

    print(f"[logly_metrics_1m] window_start={window_start} window_end={window_end}")

    # Quick sanity check: how many rows are in this window?
    ch_execute(
        f"SELECT count() FROM logly.logs WHERE received_at >= toDateTime64('{window_start}', 3, 'UTC') AND received_at < toDateTime64('{window_end}', 3, 'UTC')"
    )

    # We store ts as minute bucket start (toStartOfMinute(received_at))
    # and delete the minute bucket to make DAG idempotent.
    delete_statements = [
        f"ALTER TABLE logly.metrics_logs_1m DELETE WHERE ts = toDateTime('{window_start}', 'UTC')",
        f"ALTER TABLE logly.metrics_logs_by_level_1m DELETE WHERE ts = toDateTime('{window_start}', 'UTC')",
        f"ALTER TABLE logly.metrics_logs_by_source_1m DELETE WHERE ts = toDateTime('{window_start}', 'UTC')",
        f"ALTER TABLE logly.metrics_errors_by_type_1m DELETE WHERE ts = toDateTime('{window_start}', 'UTC')",
        f"ALTER TABLE logly.metrics_errors_by_source_type_1m DELETE WHERE ts = toDateTime('{window_start}', 'UTC')",
        f"ALTER TABLE logly.metrics_errors_by_source_1m DELETE WHERE ts = toDateTime('{window_start}', 'UTC')",
    ]

    insert_statements = [
        # 1) total logs per minute
        f"""
        INSERT INTO logly.metrics_logs_1m (ts, cnt)
        SELECT
            toStartOfMinute(received_at) AS ts,
            count() AS cnt
        FROM logly.logs
        WHERE received_at >= toDateTime64('{window_start}', 3, 'UTC')
          AND received_at <  toDateTime64('{window_end}',   3, 'UTC')
        GROUP BY ts
        """,

        # 2) logs by level per minute
        f"""
        INSERT INTO logly.metrics_logs_by_level_1m (ts, level, cnt)
        SELECT
            toStartOfMinute(received_at) AS ts,
            level,
            count() AS cnt
        FROM logly.logs
        WHERE received_at >= toDateTime64('{window_start}', 3, 'UTC')
          AND received_at <  toDateTime64('{window_end}',   3, 'UTC')
        GROUP BY ts, level
        """,

        # 3) logs by source per minute
        f"""
        INSERT INTO logly.metrics_logs_by_source_1m (ts, source, cnt)
        SELECT
            toStartOfMinute(received_at) AS ts,
            source,
            count() AS cnt
        FROM logly.logs
        WHERE received_at >= toDateTime64('{window_start}', 3, 'UTC')
          AND received_at <  toDateTime64('{window_end}',   3, 'UTC')
        GROUP BY ts, source
        """,

        # 4) errors by type per minute (only where error_type exists and non-empty)
        f"""
        INSERT INTO logly.metrics_errors_by_type_1m (ts, error_type, cnt)
        SELECT
            toStartOfMinute(received_at) AS ts,
            assumeNotNull(error_type) AS error_type,
            count() AS cnt
        FROM logly.logs
        WHERE received_at >= toDateTime64('{window_start}', 3, 'UTC')
          AND received_at <  toDateTime64('{window_end}',   3, 'UTC')
          AND error_type IS NOT NULL
          AND error_type != ''
        GROUP BY ts, error_type
        """,

        # 5) errors by source+type per minute
        f"""
        INSERT INTO logly.metrics_errors_by_source_type_1m (ts, source, error_type, cnt)
        SELECT
            toStartOfMinute(received_at) AS ts,
            source,
            assumeNotNull(error_type) AS error_type,
            count() AS cnt
        FROM logly.logs
        WHERE received_at >= toDateTime64('{window_start}', 3, 'UTC')
          AND received_at <  toDateTime64('{window_end}',   3, 'UTC')
          AND error_type IS NOT NULL
          AND error_type != ''
        GROUP BY ts, source, error_type
        """,

        # 6) errors by source per minute (variant A for anomaly chart)
        f"""
        INSERT INTO logly.metrics_errors_by_source_1m (ts, source, cnt)
        SELECT
            toStartOfMinute(received_at) AS ts,
            source,
            count() AS cnt
        FROM logly.logs
        WHERE received_at >= toDateTime64('{window_start}', 3, 'UTC')
          AND received_at <  toDateTime64('{window_end}',   3, 'UTC')
          AND (lower(level) = 'error' OR lower(level) = 'critical')
        GROUP BY ts, source
        """,
    ]

    # 7) service_last_seen: we update by inserting max(received_at) per (source, env) for the window.
    # ReplacingMergeTree(last_seen) will keep the latest value after merges.
    update_last_seen = f"""
    INSERT INTO logly.service_last_seen (source, environment, last_seen)
    SELECT
        source,
        environment,
        max(received_at) AS last_seen
    FROM logly.logs
    WHERE received_at >= toDateTime64('{window_start}', 3, 'UTC')
      AND received_at <  toDateTime64('{window_end}',   3, 'UTC')
    GROUP BY source, environment
    """

    # Execute
    for s in delete_statements:
        ch_execute(s)

    for s in insert_statements:
        ch_execute(s)

    ch_execute(update_last_seen)


# ---------- DAG ----------

default_args = {
    "owner": "logly",
    "retries": 3,
    "retry_delay": timedelta(seconds=20),
}

with DAG(
        dag_id="logly_metrics_1m",
        default_args=default_args,
        description="Build 1-minute metrics tables for Logly (except raw logs table).",
        start_date=pendulum.now("UTC").subtract(hours=1),
        schedule="*/1 * * * *",
        catchup=False,
        max_active_runs=1,
        tags=["logly", "clickhouse", "metrics"],
) as dag:

    build_metrics = PythonOperator(
        task_id="build_metrics_for_window",
        python_callable=load_metrics_for_window,
    )

    build_metrics