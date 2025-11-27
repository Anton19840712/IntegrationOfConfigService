-- ConfigurationService Database Schema
-- Таблица для хранения SIP конфигураций пользователей

CREATE TABLE IF NOT EXISTS sip_accounts (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(256) NOT NULL,
    sip_account_name VARCHAR(128) NOT NULL,
    sip_password VARCHAR(256) NOT NULL,
    display_name VARCHAR(256),
    sip_domain VARCHAR(256) NOT NULL,
    proxy_uri VARCHAR(512) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Индексы
CREATE INDEX IF NOT EXISTS idx_sip_accounts_user_id ON sip_accounts(user_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_sip_accounts_sip_account_name ON sip_accounts(sip_account_name);

-- Функция для автоматического обновления updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Триггер для автоматического обновления updated_at
CREATE TRIGGER update_sip_accounts_updated_at
    BEFORE UPDATE ON sip_accounts
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();
