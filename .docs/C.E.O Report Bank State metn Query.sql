DECLARE 
    @fromac   nvarchar(12),
    @uptoac   nvarchar(12),
    @fromdate DATE,
    @uptodate DATE;

-- values assign کریں (example)
SET @fromac = (select bank_from from acc_Desc);
SET @uptoac = (select bank_to from acc_desc);
SET @fromdate = '2023-05-01';
SET @uptodate = '2023-05-30';


;WITH acList AS (
    SELECT acc_code, acc_name
    FROM customer
    WHERE acc_code BETWEEN @fromac AND @uptoac
),

----------------------------------------
-- Previous Balance
----------------------------------------
prevBal AS (
    SELECT 
        acc_code,
        ISNULL(SUM(dr_amount) - SUM(cr_amount), 0) AS prev_balance,
        CASE 
            WHEN ISNULL(SUM(dr_amount) - SUM(cr_amount), 0) > 0 THEN 'Dr.'
            ELSE 'Cr.'
        END AS prev_type
    FROM g_journal
    WHERE acc_code BETWEEN @fromac AND @uptoac
      AND vouch_date < @fromdate
    GROUP BY acc_code
),

----------------------------------------
-- Current Transactions
----------------------------------------
currentBal AS (
    SELECT 
        acc_code,
        SUM(dr_amount) AS debit,
        SUM(cr_amount) AS credit
    FROM g_journal
    WHERE vouch_date BETWEEN @fromdate AND @uptodate
      AND acc_code BETWEEN @fromac AND @uptoac
    GROUP BY acc_code
),

----------------------------------------
-- Final Join
----------------------------------------
FinalData AS (
    SELECT 
        a.acc_code,
        a.acc_name,
        ISNULL(b.prev_balance,0) AS prev_balance,
        CASE 
            WHEN ISNULL(b.prev_balance,0) = 0 THEN ''
            ELSE b.prev_type
        END AS prev_type,
        ISNULL(c.debit,0) AS debit,
        ISNULL(c.credit,0) AS credit,
        (ISNULL(b.prev_balance,0) + ISNULL(c.debit,0)) - ISNULL(c.credit,0) AS cur_bal,
        CASE 
            WHEN (ISNULL(b.prev_balance,0) + ISNULL(c.debit,0) - ISNULL(c.credit,0)) > 0 THEN 'Dr.'
            ELSE 'Cr.'
        END AS cur_type
    FROM acList a
    LEFT JOIN prevBal b ON a.acc_code = b.acc_code
    LEFT JOIN currentBal c ON a.acc_code = c.acc_code
)

----------------------------------------
-- Final Result (DELETE logic applied)
----------------------------------------
SELECT *
FROM FinalData
WHERE NOT (
    prev_balance = 0 
    AND debit = 0 
    AND credit = 0 
    AND cur_bal = 0
)
ORDER BY acc_code;