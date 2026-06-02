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

        cmd.CommandText = @"
            INSERT INTO SESSION_DATA (session_id, customer_id, session_start, user_agent, ip_address)
            VALUES (session_seq.NEXTVAL, :customerId, CURRENT_TIMESTAMP, :userAgent, :ipAddress)";

        cmd.Parameters.Add("customerId", OracleDbType.Int64).Value = customerId;
        cmd.Parameters.Add("userAgent", OracleDbType.Varchar2).Value = userAgent;
        cmd.Parameters.Add("ipAddress", OracleDbType.Varchar2).Value = $"192.168.{new Random().Next(1, 255)}.{new Random().Next(1, 255)}";

        cmd.ExecuteNonQuery();
    }

    [Trace]
    public void EndSession(long sessionId)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = "UPDATE SESSION_DATA SET session_end = CURRENT_TIMESTAMP WHERE session_id = :sessionId";
        cmd.Parameters.Add("sessionId", OracleDbType.Int64).Value = sessionId;

        cmd.ExecuteNonQuery();
    }
}
