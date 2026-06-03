-- Sales Month Wise
WITH sales AS (
    SELECT 
        inv_date,
        MONTH(inv_date) AS sale_month,
        YEAR(inv_date) AS sales_year,
        gros_amt
    FROM sale_inv
),
salesMonthWise AS (
    SELECT 
        FORMAT(inv_date, 'MMM-yyyy') AS trans_month,
        sale_month,
        sales_year,
        SUM(gros_amt) AS sales
    FROM sales
    GROUP BY sale_month, sales_year, FORMAT(inv_date, 'MMM-yyyy')
),

-- Recovery
RecoveryTemp AS (
    SELECT 
        MONTH(vouch_date) AS rec_month,
        YEAR(vouch_date) AS rec_year,
        amount
    FROM crvoucher 
    WHERE ac_code IN (SELECT ac_code FROM sale_inv)

    UNION ALL

    SELECT 
        MONTH(vouch_date),
        YEAR(vouch_date),
        rec_amt
    FROM bank_receipt
    WHERE credit_ac IN (SELECT ac_code FROM sale_inv)

    UNION ALL

    SELECT 
        MONTH(vouch_date),
        YEAR(vouch_date),
        cr_amount
    FROM j_vouch
    WHERE cr_amount > 0 
      AND dr_amount = 0
      AND acc_code IN (SELECT ac_code FROM sale_inv)
),
recovery AS (
    SELECT 
        FORMAT(DATEFROMPARTS(rec_year, rec_month, 1), 'MMM-yyyy') AS trans_month,
        rec_month,
        rec_year,
        SUM(amount) AS rec_amt
    FROM RecoveryTemp
    GROUP BY rec_month, rec_year
),

-- Expenses
ExpTemp AS (
    SELECT 
        g.acc_code,
        MONTH(g.vouch_date) AS exp_month,
        YEAR(g.vouch_date) AS exp_year,
        g.dr_amount AS amount
    FROM g_journal g
    CROSS JOIN profit_loss p
    WHERE g.dr_amount > 0
      AND (
            g.acc_code BETWEEN p.ope_frm_Ac AND p.ope_to_ac
         OR g.acc_code BETWEEN p.man_frm_Ac AND p.man_to_ac
      )
),
expMonthWise AS (
    SELECT 
        FORMAT(DATEFROMPARTS(exp_year, exp_month, 1), 'MMM-yyyy') AS trans_month,
        exp_month,
        exp_year,
        SUM(amount) AS exp_amt
    FROM ExpTemp
    GROUP BY exp_month, exp_year
),

-- Combine All
Summary AS (
    SELECT 
        trans_month,
        sale_month AS nMonth,
        sales_year AS nYear,
        sales,
        0.0 AS rec_amt,
        0.0 AS exp_amt
    FROM salesMonthWise

    UNION ALL

    SELECT 
        trans_month,
        rec_month,
        rec_year,
        0.0,
        rec_amt,
        0.0
    FROM recovery

    UNION ALL

    SELECT 
        trans_month,
        exp_month,
        exp_year,
        0.0,
        0.0,
        exp_amt
    FROM expMonthWise
)

-- Final Result
SELECT 
    trans_month,
    SUM(sales) AS sales,
    SUM(rec_amt) AS rec_amt,
    SUM(exp_amt) AS exp_amt
FROM Summary
GROUP BY trans_month, nYear, nMonth
ORDER BY nYear, nMonth;