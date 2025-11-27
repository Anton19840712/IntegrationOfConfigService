-- ═══════════════════════════════════════════════════════════
-- ConfigurationService - SIP Account Pool Tables
-- Таблицы для управления пулом свободных SIP номеров
-- ═══════════════════════════════════════════════════════════

-- Таблица доступных SIP номеров (пул от менеджера)
CREATE TABLE IF NOT EXISTS available_sip_accounts (
    id SERIAL PRIMARY KEY,
    sip_account_name VARCHAR(128) NOT NULL UNIQUE,
    sip_password VARCHAR(256) NOT NULL,
    is_assigned BOOLEAN NOT NULL DEFAULT false,
    assigned_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Таблица ожидающих назначения (когда пул пустой)
CREATE TABLE IF NOT EXISTS pending_assignments (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(256) NOT NULL UNIQUE,
    user_login VARCHAR(128) NOT NULL,
    display_name VARCHAR(256),
    status VARCHAR(50) NOT NULL DEFAULT 'WaitingForAvailableAccount',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Индексы для оптимизации
CREATE INDEX IF NOT EXISTS idx_available_sip_accounts_is_assigned
    ON available_sip_accounts(is_assigned);

CREATE INDEX IF NOT EXISTS idx_pending_assignments_status
    ON pending_assignments(status);

CREATE INDEX IF NOT EXISTS idx_pending_assignments_created_at
    ON pending_assignments(created_at);

-- Комментарии
COMMENT ON TABLE available_sip_accounts IS 'Пул доступных SIP номеров от менеджера';
COMMENT ON COLUMN available_sip_accounts.sip_account_name IS 'SIP номер (например, 2001, 2004)';
COMMENT ON COLUMN available_sip_accounts.sip_password IS 'Пароль для SIP регистрации';
COMMENT ON COLUMN available_sip_accounts.is_assigned IS 'Назначен ли номер пользователю';
COMMENT ON COLUMN available_sip_accounts.assigned_at IS 'Дата назначения';

COMMENT ON TABLE pending_assignments IS 'Пользователи ожидающие назначения SIP номера';
COMMENT ON COLUMN pending_assignments.user_id IS 'ID пользователя из AuthService';
COMMENT ON COLUMN pending_assignments.user_login IS 'Login пользователя для удобства';
COMMENT ON COLUMN pending_assignments.status IS 'Статус: WaitingForAvailableAccount';
