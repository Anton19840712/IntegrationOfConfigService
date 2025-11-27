-- ═══════════════════════════════════════════════════════════
-- ConfigurationService - Seed Data for DEVELOPMENT
-- Тестовые SIP аккаунты для пользователей из AuthService
-- user_id и display_name взяты из AuthService
-- ═══════════════════════════════════════════════════════════

INSERT INTO sip_accounts (
    user_id,
    sip_account_name,
    sip_password,
    display_name,
    sip_domain,
    proxy_uri,
    is_active
)
VALUES
    -- admin (Role: Admin) → SIP 2004
    ('c6d620d7-2f49-4aba-b9b3-5b003a838b0b', '2004', 'elephant', 'Admin', 'sip.pbx', 'sip:172.16.211.135:5060', true),

    -- user (Role: User) → SIP 2001
    ('1f447360-71e6-4e50-a394-b2cb0f345299', '2001', 'elephant', 'User', 'sip.pbx', 'sip:172.16.211.135:5060', true)
ON CONFLICT (sip_account_name) DO NOTHING;

-- Помечаем назначенные номера как assigned в пуле
UPDATE available_sip_accounts SET is_assigned = true, assigned_at = NOW()
WHERE sip_account_name IN ('2001', '2004');

-- Проверка вставки
DO $$
DECLARE
    record_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO record_count FROM sip_accounts;
    RAISE NOTICE 'Загружено записей в sip_accounts: %', record_count;
END $$;
