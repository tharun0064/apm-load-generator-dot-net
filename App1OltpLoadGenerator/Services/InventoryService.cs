using Oracle.ManagedDataAccess.Client;
using NewRelic.Api.Agent;
using Oracle.ManagedDataAccess.Types;

namespace App1OltpLoadGenerator.Services;

public class InventoryService
{
    private readonly DatabaseManager _dbManager;

    public InventoryService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    [Trace]
    public int CheckInventory(long productId)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = "SELECT quantity_available FROM INVENTORY WHERE product_id = :productId";
        cmd.Parameters.Add("productId", OracleDbType.Int64).Value = productId;

        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    [Trace]
    public void ReserveInventory(long productId, int quantity)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = @"
            UPDATE INVENTORY
            SET quantity_available = quantity_available - :quantity,
                quantity_reserved = quantity_reserved + :quantity
            WHERE product_id = :productId AND quantity_available >= :quantity";

        cmd.Parameters.Add("quantity", OracleDbType.Int32).Value = quantity;
        cmd.Parameters.Add("productId", OracleDbType.Int64).Value = productId;

        cmd.ExecuteNonQuery();
    }

    [Trace]
    public void BulkUpdateInventory(int count)
    {
        var random = new Random();
        var productIds = Enumerable.Range(1, count).Select(_ => random.Next(500) + 1).OrderBy(x => x).ToList();

        int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                using var conn = _dbManager.GetConnection();
                using var transaction = conn.BeginTransaction();
                using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 60;
                cmd.Transaction = transaction;

                cmd.CommandText = @"
                    UPDATE INVENTORY
                    SET quantity_available = quantity_available + :quantity
                    WHERE product_id = :productId";

                cmd.Parameters.Add("quantity", OracleDbType.Int32);
                cmd.Parameters.Add("productId", OracleDbType.Int64);

                foreach (var productId in productIds)
                {
                    cmd.Parameters["quantity"].Value = random.Next(10, 100);
                    cmd.Parameters["productId"].Value = productId;
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return; // Success
            }
            catch (OracleException ex) when (ex.Number == 60) // ORA-00060 deadlock
            {
                if (attempt < maxRetries - 1)
                {
                    int backoffMs = 100 * (1 << attempt);
                    Thread.Sleep(backoffMs);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
