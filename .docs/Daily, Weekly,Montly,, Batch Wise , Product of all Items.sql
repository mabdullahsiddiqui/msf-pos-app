declare @dateFrom as date,@dateto as date;
set @datefrom ='2025/06/01' ;
set @dateto ='2025/06/30' ;

WITH temp AS (
    SELECT 
        LEFT(item_code, 2) AS prod_group,
        wght_prod
    FROM batch_production
    WHERE prod_date BETWEEN @dateFrom  and @dateto
)

SELECT 
    a.prod_group,
    MAX(b.group_name) AS group_name,
    ROUND(SUM(a.wght_prod) * 100.0 / 
        (SELECT SUM(wght_prod) FROM temp), 2) AS per
FROM temp a
JOIN item_group b 
    ON a.prod_group = b.group_code
GROUP BY a.prod_group;