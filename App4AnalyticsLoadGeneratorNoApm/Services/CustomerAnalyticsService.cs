using Oracle.ManagedDataAccess.Client;

namespace App2AnalyticsLoadGenerator.Services;

public class CustomerAnalyticsService
{
    private readonly DatabaseManager _dbManager;

    public CustomerAnalyticsService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    public void GetCustomerRetentionRate()
    {
        string sql = @"
            WITH monthly_customers AS (
                SELECT TO_CHAR(o.order_date, 'YYYY-MM') as month,
                       o.customer_id
                FROM oltp_user.ORDERS o
                WHERE o.order_date >= ADD_MONTHS(SYSDATE, -12)
                GROUP BY TO_CHAR(o.order_date, 'YYYY-MM'), o.customer_id
            ),
            retention_calc AS (
                SELECT mc1.month,
                       COUNT(DISTINCT mc1.customer_id) as customers_this_month,
                       COUNT(DISTINCT mc2.customer_id) as customers_next_month
                FROM monthly_customers mc1
                LEFT JOIN monthly_customers mc2
                    ON mc2.month = TO_CHAR(ADD_MONTHS(TO_DATE(mc1.month, 'YYYY-MM'), 1), 'YYYY-MM')
                    AND mc1.customer_id = mc2.customer_id
                GROUP BY mc1.month
            )
            SELECT month,
                   customers_this_month,
                   customers_next_month,
                   ROUND(customers_next_month * 100.0 / NULLIF(customers_this_month, 0), 2) as retention_rate
            FROM retention_calc
            ORDER BY month DESC";

        ExecuteQuery(sql, "CustomerRetentionRate");
    }

    private void ExecuteQuery(string sql, string queryName)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 180;
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();
        int rowCount = 0;
        while (reader.Read()) rowCount++;
        Console.WriteLine($"{queryName}: {rowCount} rows");
    }
}
