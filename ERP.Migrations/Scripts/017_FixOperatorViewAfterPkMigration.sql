-- Naprawa widoku v_operator_permissions_summary po migracji operator.id_operatora -> operator.id
-- Uruchomić po wykonaniu etap3_klasa_C_pk_do_id.sql (zmiana PK operator na id)

CREATE OR REPLACE VIEW v_operator_permissions_summary AS
SELECT 
    o.id AS id_operatora,
    o.imie_nazwisko,
    otp.table_name,
    otp.can_select,
    otp.can_insert,
    otp.can_update,
    otp.can_delete,
    otp.UpdatedAt
FROM operator o
LEFT JOIN operator_table_permissions otp ON o.id = otp.id_operatora
ORDER BY o.imie_nazwisko, otp.table_name;
