using Oracle.ManagedDataAccess.Client;
using NewRelic.Api.Agent;

namespace App2AnalyticsLoadGenerator.Services;

public class ProductAnalyticsService
{
    private readonly DatabaseManager _dbManager;

    public ProductAnalyticsService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    [Trace]
    public void GetProductPerformanceReport()
    {
        string sql = @"
            SELECT p.product_id,
                   p.product_name,
                   p.category,
                   COALESCE(SUM(oi.quantity), 0) as total_units_sold,
                   COALESCE(SUM(oi.subtotal), 0) as total_revenue
            FROM oltp_user.PRODUCTS p
            LEFT JOIN oltp_user.ORDER_ITEMS oi ON p.product_id = oi.product_id
            LEFT JOIN oltp_user.ORDERS o ON oi.order_id = o.order_id AND o.order_date >= SYSDATE - 90
            WHERE p.is_active = 1
            GROUP BY p.product_id, p.product_name, p.category
            ORDER BY total_revenue DESC";

        ExecuteQuery(sql, "ProductPerformanceReport");
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
