using Oracle.ManagedDataAccess.Client;
using NewRelic.Api.Agent;

namespace App1OltpLoadGenerator.Services;

public class CustomerService
{
    private readonly DatabaseManager _dbManager;

    public CustomerService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    [Trace]
    public void UpdateLoyaltyPoints(long customerId, int points)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = @"
            UPDATE CUSTOMERS
            SET loyalty_points = loyalty_points + :points
            WHERE customer_id = :customerId";

        cmd.Parameters.Add("points", OracleDbType.Int32).Value = points;
        cmd.Parameters.Add("customerId", OracleDbType.Int64).Value = customerId;

        cmd.ExecuteNonQuery();
    }

    [Trace]
    public void UpdateCustomerType(long customerId, string customerType)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = "UPDATE CUSTOMERS SET customer_type = :customerType WHERE customer_id = :customerId";
        cmd.Parameters.Add("customerType", OracleDbType.Varchar2).Value = customerType;
        cmd.Parameters.Add("customerId", OracleDbType.Int64).Value = customerId;

        cmd.ExecuteNonQuery();
    }

    [Trace]
    public Dictionary<string, object>? GetCustomerDetails(long customerId)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = @"
            SELECT customer_id, first_name, last_name, email, customer_type, loyalty_points
            FROM CUSTOMERS
            WHERE customer_id = :customerId";

        cmd.Parameters.Add("customerId", OracleDbType.Int64).Value = customerId;

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Dictionary<string, object>
            {
                ["customer_id"] = reader.GetInt64(0),
                ["first_name"] = reader.GetString(1),
                ["last_name"] = reader.GetString(2),
                ["email"] = reader.GetString(3),
                ["customer_type"] = reader.GetString(4),
                ["loyalty_points"] = reader.GetInt32(5)
            };
        }

        return null;
    }
}
