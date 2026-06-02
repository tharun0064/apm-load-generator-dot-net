using Oracle.ManagedDataAccess.Client;
using NewRelic.Api.Agent;
using System.Data;

namespace App1OltpLoadGenerator.Services;

public class OrderService
{
    private readonly DatabaseManager _dbManager;

    public OrderService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    [Trace]
    public long CreateOrder(long customerId, string paymentMethod = "CREDIT_CARD")
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = @"
            INSERT INTO ORDERS (order_id, customer_id, order_date, status, payment_method, created_at)
            VALUES (order_seq.NEXTVAL, :customerId, CURRENT_TIMESTAMP, 'PENDING', :paymentMethod, CURRENT_TIMESTAMP)
            RETURNING order_id INTO :orderId";

        cmd.Parameters.Add("customerId", OracleDbType.Int64).Value = customerId;
        cmd.Parameters.Add("paymentMethod", OracleDbType.Varchar2).Value = paymentMethod;

        var orderIdParam = new OracleParameter("orderId", OracleDbType.Int64) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(orderIdParam);

        cmd.ExecuteNonQuery();

        return Convert.ToInt64(orderIdParam.Value.ToString());
    }

    [Trace]
    public void AddOrderItem(long orderId, long productId, int quantity, decimal price)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        decimal subtotal = quantity * price;

        cmd.CommandText = @"
            INSERT INTO ORDER_ITEMS (order_item_id, order_id, product_id, quantity, unit_price, subtotal)
            VALUES (order_item_seq.NEXTVAL, :orderId, :productId, :quantity, :price, :subtotal)";

        cmd.Parameters.Add("orderId", OracleDbType.Int64).Value = orderId;
        cmd.Parameters.Add("productId", OracleDbType.Int64).Value = productId;
        cmd.Parameters.Add("quantity", OracleDbType.Int32).Value = quantity;
        cmd.Parameters.Add("price", OracleDbType.Decimal).Value = price;
        cmd.Parameters.Add("subtotal", OracleDbType.Decimal).Value = subtotal;

        cmd.ExecuteNonQuery();
    }

    [Trace]
    public void UpdateOrderTotal(long orderId, decimal totalAmount, decimal taxAmount, decimal shippingCost)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = @"
            UPDATE ORDERS
            SET total_amount = :totalAmount, tax_amount = :taxAmount, shipping_cost = :shippingCost
            WHERE order_id = :orderId";

        cmd.Parameters.Add("totalAmount", OracleDbType.Decimal).Value = totalAmount;
        cmd.Parameters.Add("taxAmount", OracleDbType.Decimal).Value = taxAmount;
        cmd.Parameters.Add("shippingCost", OracleDbType.Decimal).Value = shippingCost;
        cmd.Parameters.Add("orderId", OracleDbType.Int64).Value = orderId;

        cmd.ExecuteNonQuery();
    }

    [Trace]
    public void UpdateOrderStatus(long orderId, string status)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = "UPDATE ORDERS SET status = :status WHERE order_id = :orderId";
        cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = status;
        cmd.Parameters.Add("orderId", OracleDbType.Int64).Value = orderId;

        cmd.ExecuteNonQuery();
    }

    [Trace]
    public int DeleteOldCompletedOrders(int daysToKeep)
    {
        using var conn = _dbManager.GetConnection();
        using var transaction = conn.BeginTransaction();

        try
        {
            // Delete ORDER_ITEMS first (FK constraint)
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = 120;
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    DELETE FROM ORDER_ITEMS
                    WHERE order_id IN (
                        SELECT order_id FROM ORDERS
                        WHERE status IN ('COMPLETED', 'DELIVERED') AND order_date < SYSDATE - :daysToKeep
                    )";
                cmd.Parameters.Add("daysToKeep", OracleDbType.Int32).Value = daysToKeep;
                cmd.ExecuteNonQuery();
            }

            // Delete TRANSACTIONS
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = 120;
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    DELETE FROM TRANSACTIONS
                    WHERE order_id IN (
                        SELECT order_id FROM ORDERS
                        WHERE status IN ('COMPLETED', 'DELIVERED') AND order_date < SYSDATE - :daysToKeep
                    )";
                cmd.Parameters.Add("daysToKeep", OracleDbType.Int32).Value = daysToKeep;
                cmd.ExecuteNonQuery();
            }

            // Delete ORDERS
            int deletedCount;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = 120;
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    DELETE FROM ORDERS
                    WHERE status IN ('COMPLETED', 'DELIVERED') AND order_date < SYSDATE - :daysToKeep";
                cmd.Parameters.Add("daysToKeep", OracleDbType.Int32).Value = daysToKeep;
                deletedCount = cmd.ExecuteNonQuery();
            }

            transaction.Commit();
            return deletedCount;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
