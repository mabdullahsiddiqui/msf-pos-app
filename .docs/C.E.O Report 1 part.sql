DECLARE @tran_date DATE = '2023/06/26'  -- اپنی date بھی دے سکتے ہیں

;WITH SummaryTrans AS (
    SELECT 1 AS sr, 'Today' AS legend
    UNION ALL SELECT 2, 'Yesterday'
    UNION ALL SELECT 3, 'This Week'
    UNION ALL SELECT 4, 'Last Week'
    UNION ALL SELECT 5, 'This Month'
    UNION ALL SELECT 6, 'Last Month'
    UNION ALL SELECT 7, 'Year To Date'
),

----------------------------------------
-- SALES
----------------------------------------
SalesAgg AS (
    SELECT 1 AS t_id, SUM(gros_amt) AS sales FROM sale_inv WHERE inv_date = @tran_date
    UNION ALL
    SELECT 2, SUM(gros_amt) FROM sale_inv WHERE inv_date = DATEADD(DAY,-1,@tran_date)
    UNION ALL
    SELECT 3, SUM(gros_amt) FROM sale_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,@tran_date)
    UNION ALL
    SELECT 4, SUM(gros_amt) FROM sale_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,DATEADD(WEEK,-1,@tran_date))
    UNION ALL
    SELECT 5, SUM(gros_amt) FROM sale_inv WHERE MONTH(inv_date)=MONTH(@tran_date) AND YEAR(inv_date)=YEAR(@tran_date)
    UNION ALL
    SELECT 6, SUM(gros_amt) FROM sale_inv WHERE MONTH(inv_date)=MONTH(DATEADD(MONTH,-1,@tran_date)) AND YEAR(inv_date)=YEAR(DATEADD(MONTH,-1,@tran_date))
    UNION ALL
    SELECT 7, SUM(gros_amt) FROM sale_inv
),

----------------------------------------
-- SALES RETURN
----------------------------------------
SalesRetAgg AS (
    SELECT 1 AS t_id, SUM(gros_amt) AS sales_ret FROM saleret_inv WHERE inv_date = @tran_date
    UNION ALL
    SELECT 2, SUM(gros_amt) FROM saleret_inv WHERE inv_date = DATEADD(DAY,-1,@tran_date)
    UNION ALL
    SELECT 3, SUM(gros_amt) FROM saleret_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,@tran_date)
    UNION ALL
    SELECT 4, SUM(gros_amt) FROM saleret_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,DATEADD(WEEK,-1,@tran_date))
    UNION ALL
    SELECT 5, SUM(gros_amt) FROM saleret_inv WHERE MONTH(inv_date)=MONTH(@tran_date) AND YEAR(inv_date)=YEAR(@tran_date)
    UNION ALL
    SELECT 6, SUM(gros_amt) FROM saleret_inv WHERE MONTH(inv_date)=MONTH(DATEADD(MONTH,-1,@tran_date)) AND YEAR(inv_date)=YEAR(DATEADD(MONTH,-1,@tran_date))
    UNION ALL
    SELECT 7, SUM(gros_amt) FROM saleret_inv
),

----------------------------------------
-- RECOVERY
----------------------------------------
RecoveryData AS (
    SELECT vouch_date, amount FROM crvoucher WHERE ac_code IN (SELECT ac_code FROM sale_inv)
    UNION ALL
    SELECT vouch_date, rec_amt FROM bank_receipt WHERE credit_ac IN (SELECT ac_code FROM sale_inv)
    UNION ALL
    SELECT vouch_date, cr_amount FROM j_vouch 
    WHERE cr_amount > 0 AND dr_amount = 0 AND acc_code IN (SELECT ac_code FROM sale_inv)
),
RecoveryAgg AS (
    SELECT 1 AS t_id, SUM(amount) AS recovery FROM RecoveryData WHERE vouch_date=@tran_date
    UNION ALL
    SELECT 2, SUM(amount) FROM RecoveryData WHERE vouch_date=DATEADD(DAY,-1,@tran_date)
    UNION ALL
    SELECT 3, SUM(amount) FROM RecoveryData WHERE DATEPART(WEEK,vouch_date)=DATEPART(WEEK,@tran_date)
    UNION ALL
    SELECT 4, SUM(amount) FROM RecoveryData WHERE DATEPART(WEEK,vouch_date)=DATEPART(WEEK,DATEADD(WEEK,-1,@tran_date))
    UNION ALL
    SELECT 5, SUM(amount) FROM RecoveryData WHERE MONTH(vouch_date)=MONTH(@tran_date) AND YEAR(vouch_date)=YEAR(@tran_date)
    UNION ALL
    SELECT 6, SUM(amount) FROM RecoveryData WHERE MONTH(vouch_date)=MONTH(DATEADD(MONTH,-1,@tran_date)) AND YEAR(vouch_date)=YEAR(DATEADD(MONTH,-1,@tran_date))
    UNION ALL
    SELECT 7, SUM(amount) FROM RecoveryData
),

----------------------------------------
-- PURCHASE
----------------------------------------
PurchaseAgg AS (
    SELECT 1 AS t_id, SUM(gross_amt) AS purchase FROM pur_inv WHERE inv_date=@tran_date
    UNION ALL
    SELECT 2, SUM(gross_amt) FROM pur_inv WHERE inv_date=DATEADD(DAY,-1,@tran_date)
    UNION ALL
    SELECT 3, SUM(gross_amt) FROM pur_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,@tran_date)
    UNION ALL
    SELECT 4, SUM(gross_amt) FROM pur_inv WHERE DATEPART(WEEK,inv_date)=DATEPART(WEEK,DATEADD(WEEK,-1,@tran_date))
    UNION ALL
    SELECT 5, SUM(gross_amt) FROM pur_inv WHERE MONTH(inv_date)=MONTH(@tran_date) AND YEAR(inv_date)=YEAR(@tran_date)
    UNION ALL
    SELECT 6, SUM(gross_amt) FROM pur_inv WHERE MONTH(inv_date)=MONTH(DATEADD(MONTH,-1,@tran_date)) AND YEAR(inv_date)=YEAR(DATEADD(MONTH,-1,@tran_date))
    UNION ALL
    SELECT 7, SUM(gross_amt) FROM pur_inv
),

----------------------------------------
-- FINAL RESULT
----------------------------------------
FinalData AS (
    SELECT 
        a.sr,
        a.legend,
        ISNULL(b.sales,0) AS sales,
        ISNULL(c.sales_ret,0) AS sales_ret,
        ISNULL(b.sales,0) - ISNULL(c.sales_ret,0) AS net_sales,
        ISNULL(d.recovery,0) AS recovery,
        ISNULL(e.purchase,0) AS purchase
    FROM SummaryTrans a
    LEFT JOIN SalesAgg b ON a.sr=b.t_id
    LEFT JOIN SalesRetAgg c ON a.sr=c.t_id
    LEFT JOIN RecoveryAgg d ON a.sr=d.t_id
    LEFT JOIN PurchaseAgg e ON a.sr=e.t_id
)

SELECT * FROM FinalData
ORDER BY sr;