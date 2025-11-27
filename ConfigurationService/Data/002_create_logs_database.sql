-- ═══════════════════════════════════════════════════════════
-- Создание базы данных для централизованных логов (Serilog)
-- Выполняется в postgres-config контейнере
-- ═══════════════════════════════════════════════════════════

-- Создание БД для логов
CREATE DATABASE logsdb
    WITH
    OWNER = config_admin
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TEMPLATE = template0;

\c logsdb;

-- Таблица создастся автоматически через Serilog (needAutoCreateTable: true)
-- Но можно создать вручную для контроля:

CREATE TABLE IF NOT EXISTS logs (
    id SERIAL PRIMARY KEY,
    message TEXT,
    message_template TEXT,
    level VARCHAR(50),
    timestamp TIMESTAMPTZ NOT NULL,
    exception TEXT,
    log_event JSONB,
    service_name TEXT,
    environment TEXT
);

-- Индексы для быстрого поиска
CREATE INDEX IF NOT EXISTS idx_logs_timestamp ON logs(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_logs_service ON logs(service_name);
CREATE INDEX IF NOT EXISTS idx_logs_level ON logs(level);
CREATE INDEX IF NOT EXISTS idx_logs_service_timestamp ON logs(service_name, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_logs_event_gin ON logs USING gin(log_event);

-- Комментарии
COMMENT ON TABLE logs IS 'Централизованные логи всех микросервисов (Serilog)';
COMMENT ON COLUMN logs.message IS 'Отформатированное сообщение лога';
COMMENT ON COLUMN logs.message_template IS 'Шаблон сообщения с placeholder-ами';
COMMENT ON COLUMN logs.level IS 'Уровень: Debug, Information, Warning, Error, Fatal';
COMMENT ON COLUMN logs.timestamp IS 'Время лога (UTC)';
COMMENT ON COLUMN logs.exception IS 'Stack trace ошибки';
COMMENT ON COLUMN logs.log_event IS 'Полный structured log event (JSON)';
COMMENT ON COLUMN logs.service_name IS 'Имя микросервиса';
COMMENT ON COLUMN logs.environment IS 'Окружение: Development, Production';

\echo '═══════════════════════════════════════════════════════════'
\echo 'База данных logsdb для централизованных логов создана'
\echo 'Все микросервисы будут писать логи в таблицу logs'
\echo '═══════════════════════════════════════════════════════════'
