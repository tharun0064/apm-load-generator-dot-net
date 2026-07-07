using Oracle.ManagedDataAccess.Client;

namespace App2AnalyticsLoadGenerator.Services;

public class CustomerDataService
{
    private readonly DatabaseManager _dbManager;

    public CustomerDataService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    public List<Dictionary<string, object>> GetCustomerAnalytics()
    {
        // This is the HEAVIEST query (5-10 seconds) - preserve exact Oracle SQL
        string sql = @"
            SELECT /*+ FULL(c) FULL(o) FULL(oi) FULL(p) FULL(inv) FULL(t) */
                c.customer_id,
                c.first_name || ' ' || c.last_name as customer_name,
                c.email,
                c.customer_type,
                c.loyalty_points,
                COUNT(*) OVER (PARTITION BY c.customer_id) as customer_total_orders,
                SUM(o.total_amount) OVER (PARTITION BY c.customer_id) as customer_lifetime_value,
                RANK() OVER (PARTITION BY c.customer_id ORDER BY o.order_date DESC) as customer_order_rank,
                o.order_id,
                o.order_date,
                o.status as order_status,
                o.total_amount as order_total,
                o.payment_method,
                oi.product_id,
                p.product_name,
                p.category as product_category,
                oi.quantity,
                oi.unit_price,
                oi.subtotal,
                inv.quantity_available as inventory_available,
                t.transaction_id,
                t.status as transaction_status,
                t.payment_gateway,
                CASE
                    WHEN o.order_date >= ADD_MONTHS(SYSDATE, -1) THEN 'RECENT'
                    WHEN o.order_date >= ADD_MONTHS(SYSDATE, -6) THEN 'MODERATE'
                    ELSE 'OLD'
                END as order_recency,
                CASE
                    WHEN SUM(o.total_amount) OVER (PARTITION BY c.customer_id) >= 1000 THEN 'HIGH_VALUE'
                    WHEN SUM(o.total_amount) OVER (PARTITION BY c.customer_id) >= 500 THEN 'MEDIUM_VALUE'
                    ELSE 'LOW_VALUE'
                END as customer_value_segment
            FROM oltp_user.CUSTOMERS c,
                 oltp_user.ORDERS o,
                 oltp_user.ORDER_ITEMS oi,
                 oltp_user.PRODUCTS p,
                 oltp_user.INVENTORY inv,
                 oltp_user.TRANSACTIONS t
            WHERE c.customer_id = o.customer_id
              AND o.order_id = oi.order_id
              AND oi.product_id = p.product_id
              AND p.product_id = inv.product_id(+)
              AND o.order_id = t.order_id(+)
              AND o.order_date >= ADD_MONTHS(SYSDATE, -24)
            ORDER BY c.customer_id, o.order_date DESC, oi.subtotal DESC
            FETCH FIRST 500 ROWS ONLY";

        return ExecuteAnalyticsQuery(sql, "CustomerAnalytics");
    }

    private List<Dictionary<string, object>> ExecuteAnalyticsQuery(string sql, string queryName)
    {
        long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var results = new List<Dictionary<string, object>>();

        try
        {
            using var conn = _dbManager.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 180; // 3 minutes for heavy queries
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            int rowCount = 0;
            while (reader.Read())
            {
                rowCount++;
                // Don't materialize data, just count rows
            }

            long duration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startTime;
            Console.WriteLine($"{queryName} query returned {rowCount} rows in {duration}ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing {queryName} query: {ex.Message}");
            throw;
        }

        return results;
    }
}
