using Oracle.ManagedDataAccess.Client;

namespace App2AnalyticsLoadGenerator.Services;

public class DataWarehouseService
{
    private readonly DatabaseManager _dbManager;

    public DataWarehouseService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    public void AggregateSalesData()
    {
        string sql = @"
            MERGE INTO analytics_user.SALES_SUMMARY ss
            USING (
                SELECT TRUNC(o.order_date) as summary_date,
                       COUNT(DISTINCT o.order_id) as total_orders,
                       SUM(o.total_amount) as total_revenue
                FROM oltp_user.ORDERS o
                WHERE o.order_date >= TRUNC(SYSDATE) - 7
                  AND o.status IN ('COMPLETED', 'SHIPPED', 'DELIVERED')
                GROUP BY TRUNC(o.order_date)
            ) src
            ON (ss.summary_date = src.summary_date)
            WHEN MATCHED THEN
                UPDATE SET ss.total_orders = src.total_orders,
                           ss.total_revenue = src.total_revenue
            WHEN NOT MATCHED THEN
                INSERT (summary_id, summary_date, total_orders, total_revenue)
                VALUES (sales_summary_seq.NEXTVAL, src.summary_date, src.total_orders, src.total_revenue)";

        ExecuteUpdate(sql, "AggregateSalesData");
    }

    public void PerformComplexJoinQuery()
    {
        string sql = @"
            SELECT c.customer_id,
                   c.first_name || ' ' || c.last_name as customer_name,
                   COUNT(DISTINCT o.order_id) as order_count,
                   SUM(oi.quantity) as total_items
            FROM oltp_user.CUSTOMERS c
            LEFT JOIN oltp_user.ORDERS o ON c.customer_id = o.customer_id
            LEFT JOIN oltp_user.ORDER_ITEMS oi ON o.order_id = oi.order_id
            WHERE o.order_date >= SYSDATE - 90
            GROUP BY c.customer_id, c.first_name, c.last_name
            FETCH FIRST 100 ROWS ONLY";

        ExecuteQuery(sql, "ComplexJoinQuery");
    }

    private void ExecuteUpdate(string sql, string operationName)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 180;
        cmd.CommandText = sql;
        int affected = cmd.ExecuteNonQuery();
        Console.WriteLine($"{operationName}: {affected} rows affected");
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
