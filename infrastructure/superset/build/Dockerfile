FROM apache/superset:4.1.4

USER root
RUN pip install --no-cache-dir "SQLAlchemy<2" clickhouse-connect clickhouse-sqlalchemy

COPY init.sh /app/superset-init.sh
RUN chmod +x /app/superset-init.sh

USER superset
ENTRYPOINT ["/bin/bash", "/app/superset-init.sh"]