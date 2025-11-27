-- ═══════════════════════════════════════════════════════════
-- ConfigurationService - Seed SIP Pool
-- Начальный пул SIP номеров от менеджера
-- ═══════════════════════════════════════════════════════════

INSERT INTO available_sip_accounts (
    sip_account_name,
    sip_password,
    is_assigned
)
VALUES
    -- Номера уже существующие в Asterisk
    ('2001', 'elephant', false),
    ('2002', 'elephant', false),
    ('2003', 'elephant', false),
    ('2004', 'elephant', false),
    ('2005', 'elephant', false),
    ('2006', 'elephant', false)
ON CONFLICT (sip_account_name) DO NOTHING;

-- Проверка вставки
DO $$
DECLARE
    total_count INTEGER;
    available_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO total_count FROM available_sip_accounts;
    SELECT COUNT(*) INTO available_count FROM available_sip_accounts WHERE is_assigned = false;

    RAISE NOTICE '═══════════════════════════════════════════════════════════';
    RAISE NOTICE 'SIP Account Pool initialized';
    RAISE NOTICE 'Total accounts: %', total_count;
    RAISE NOTICE 'Available accounts: %', available_count;
    RAISE NOTICE '═══════════════════════════════════════════════════════════';
END $$;
