import os
from cachelib.redis import RedisCache

#
# Core
#
SECRET_KEY = os.getenv("SUPERSET_SECRET_KEY", "logly-superset-secret-key-change-me")
WTF_CSRF_ENABLED = True

# In Docker it's convenient to trust the reverse-proxy/compose network.
# You can tighten this later if you want.
ENABLE_PROXY_FIX = True

#
# Redis (cache + celery)
#
REDIS_HOST = os.getenv("REDIS_HOST", "superset-redis")
REDIS_PORT = int(os.getenv("REDIS_PORT", "6379"))
REDIS_DB = int(os.getenv("REDIS_DB", "0"))

# Cache (charts, metadata)
CACHE_CONFIG = {
    "CACHE_TYPE": "RedisCache",
    "CACHE_DEFAULT_TIMEOUT": 300,
    "CACHE_KEY_PREFIX": "superset_cache_",
    "CACHE_REDIS_HOST": REDIS_HOST,
    "CACHE_REDIS_PORT": REDIS_PORT,
    "CACHE_REDIS_DB": REDIS_DB,
}

# Results backend for SQL Lab
RESULTS_BACKEND = RedisCache(
    host=REDIS_HOST,
    port=REDIS_PORT,
    db=REDIS_DB,
    key_prefix="superset_results_",
)

#
# Celery (optional, but good to have; will still work without a dedicated worker)
#
class CeleryConfig:
    broker_url = f"redis://{REDIS_HOST}:{REDIS_PORT}/1"
    result_backend = f"redis://{REDIS_HOST}:{REDIS_PORT}/2"
    task_acks_late = True
    task_annotations = {"sql_lab.get_sql_results": {"rate_limit": "50/s"}}

CELERY_CONFIG = CeleryConfig

#
# UX defaults
#
FEATURE_FLAGS = {
    # Helpful for SQL Lab / dataset exploration
    "SQLLAB_BACKEND_PERSISTENCE": True,
}

# If you ever open embedded dashboards, keep this False by default
EMBEDDED_SUPERSET = False

#
# Logging (optional)
#
LOG_LEVEL = os.getenv("SUPERSET_LOG_LEVEL", "INFO")