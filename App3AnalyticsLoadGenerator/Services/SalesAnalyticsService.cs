using Oracle.ManagedDataAccess.Client;
using NewRelic.Api.Agent;

namespace App2AnalyticsLoadGenerator.Services;

public class SalesAnalyticsService
{
    private readonly DatabaseManager _dbManager;

    public SalesAnalyticsService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    [Trace]
    public void GetDailySalesSummary()
    {
        string sql = @"
            SELECT TRUNC(o.order_date) as order_day,
                   COUNT(DISTINCT o.order_id) as total_orders,
                   SUM(o.total_amount) as revenue
            FROM oltp_user.ORDERS o
            JOIN oltp_user.ORDER_ITEMS oi ON o.order_id = oi.order_id
            WHERE o.order_date >= SYSDATE - 30
            GROUP BY TRUNC(o.order_date)
            ORDER BY order_day DESC";

        ExecuteQuery(sql, "DailySalesSummary");
    }

    [Trace]
    public void GetMonthlySalesSummary()
    {
        string sql = @"
            SELECT TO_CHAR(o.order_date, 'YYYY-MM') as order_month,
                   COUNT(DISTINCT o.order_id) as total_orders,
                   SUM(o.total_amount) as revenue
            FROM oltp_user.ORDERS o
            WHERE o.order_date >= ADD_MONTHS(SYSDATE, -12)
            GROUP BY TO_CHAR(o.order_date, 'YYYY-MM')
            ORDER BY order_month DESC";

        ExecuteQuery(sql, "MonthlySalesSummary");
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
