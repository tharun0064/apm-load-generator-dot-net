using Oracle.ManagedDataAccess.Client;
using NewRelic.Api.Agent;

namespace App2AnalyticsLoadGenerator.Services;

public class ReportingService
{
    private readonly DatabaseManager _dbManager;

    public ReportingService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    [Trace]
    public void GenerateExecutiveDashboard()
    {
        string sql = @"
            SELECT 'Orders' as metric,
                   (SELECT COUNT(*) FROM oltp_user.ORDERS WHERE TRUNC(order_date) = TRUNC(SYSDATE)) as today_count,
                   (SELECT COUNT(*) FROM oltp_user.ORDERS WHERE TRUNC(order_date) = TRUNC(SYSDATE - 1)) as yesterday_count
            FROM DUAL
            UNION ALL
            SELECT 'Revenue' as metric,
                   ROUND((SELECT SUM(total_amount) FROM oltp_user.ORDERS WHERE TRUNC(order_date) = TRUNC(SYSDATE)), 0),
                   ROUND((SELECT SUM(total_amount) FROM oltp_user.ORDERS WHERE TRUNC(order_date) = TRUNC(SYSDATE - 1)), 0)
            FROM DUAL";

        ExecuteQuery(sql, "ExecutiveDashboard");
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
