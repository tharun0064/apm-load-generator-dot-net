using Oracle.ManagedDataAccess.Client;
using NewRelic.Api.Agent;

namespace App1OltpLoadGenerator.Services;

public class SessionService
{
    private readonly DatabaseManager _dbManager;

    public SessionService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    [Trace]
    public void CreateSession(long customerId, string userAgent)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        // session_id is a VARCHAR2 primary key (a UUID in the reference impl), not a sequence.
        cmd.CommandText = @"
            INSERT INTO SESSION_DATA (session_id, customer_id, login_time, last_activity, user_agent, ip_address, is_active)
            VALUES (:sessionId, :customerId, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, :userAgent, :ipAddress, 1)";

        cmd.Parameters.Add("sessionId", OracleDbType.Varchar2).Value = Guid.NewGuid().ToString();
        cmd.Parameters.Add("customerId", OracleDbType.Int64).Value = customerId;
        cmd.Parameters.Add("userAgent", OracleDbType.Varchar2).Value = userAgent;
        cmd.Parameters.Add("ipAddress", OracleDbType.Varchar2).Value = $"192.168.{new Random().Next(1, 255)}.{new Random().Next(1, 255)}";

        cmd.ExecuteNonQuery();
    }

    [Trace]
    public void EndSession(string sessionId)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = "UPDATE SESSION_DATA SET is_active = 0 WHERE session_id = :sessionId";
        cmd.Parameters.Add("sessionId", OracleDbType.Varchar2).Value = sessionId;

        cmd.ExecuteNonQuery();
    }
}
