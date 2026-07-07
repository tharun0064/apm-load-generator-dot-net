using Oracle.ManagedDataAccess.Client;
using NewRelic.Api.Agent;

namespace App1OltpLoadGenerator.Services;

public class TableCleanupService
{
    private readonly DatabaseManager _dbManager;

    public TableCleanupService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    [Trace]
    public void TruncateAndRebuild()
    {
        Console.WriteLine("Starting table cleanup and rebuild...");

        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 180; // 3 minutes

        // 1. Disable foreign key constraints (best-effort)
        try
        {
            Console.WriteLine("Disabling foreign key constraints...");
            cmd.CommandText = @"
                BEGIN
                    FOR c IN (SELECT constraint_name, table_name FROM user_constraints WHERE constraint_type = 'R') LOOP
                        EXECUTE IMMEDIATE 'ALTER TABLE ' || c.table_name || ' DISABLE CONSTRAINT ' || c.constraint_name;
                    END LOOP;
                END;";
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Could not disable foreign key constraints (continuing): {ex.Message}");
        }

        // 2. Truncate tables in dependency order. Best-effort: a table that is
        //    momentarily locked by another session (ORA-00054) is logged and
        //    skipped so startup is never blocked; the next cleanup cycle retries.
        Console.WriteLine("Truncating tables...");
        string[] tables = { "ORDER_ITEMS", "TRANSACTIONS", "ORDERS", "SESSION_DATA", "AUDIT_LOG", "INVENTORY", "CUSTOMERS", "PRODUCTS" };
        int truncated = 0;
        foreach (var table in tables)
        {
            try
            {
                cmd.CommandText = $"TRUNCATE TABLE {table}";
                cmd.ExecuteNonQuery();
                truncated++;
                Console.WriteLine($"  Truncated {table}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Failed to truncate {table} (continuing): {ex.Message}");
            }
        }

        // 3. Re-enable foreign key constraints (best-effort)
        try
        {
            Console.WriteLine("Re-enabling foreign key constraints...");
            cmd.CommandText = @"
                BEGIN
                    FOR c IN (SELECT constraint_name, table_name FROM user_constraints WHERE constraint_type = 'R') LOOP
                        EXECUTE IMMEDIATE 'ALTER TABLE ' || c.table_name || ' ENABLE CONSTRAINT ' || c.constraint_name;
                    END LOOP;
                END;";
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Could not re-enable foreign key constraints (continuing): {ex.Message}");
        }

        Console.WriteLine($"Table cleanup completed: {truncated}/{tables.Length} tables truncated");

        // 4. Repopulate seed data (best-effort — never block startup)
        try
        {
            Console.WriteLine("Repopulating seed data...");
            RepopulateSeedData(conn);
            Console.WriteLine("Table cleanup and rebuild completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error repopulating seed data (continuing): {ex.Message}");
        }
    }

    private void RepopulateSeedData(OracleConnection conn)
    {
        var random = new Random();

        // Repopulate customers (1000 customers)
        Console.WriteLine("  Inserting 1000 customers...");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO CUSTOMERS (customer_id, first_name, last_name, email, phone, address, city, state, zip_code, country, customer_type, loyalty_points, created_at)
                VALUES (customer_seq.NEXTVAL, :first_name, :last_name, :email, :phone, :address, :city, :state, :zip_code, :country, :customer_type, :loyalty_points, CURRENT_TIMESTAMP)";

            cmd.Parameters.Add("first_name", OracleDbType.Varchar2);
            cmd.Parameters.Add("last_name", OracleDbType.Varchar2);
            cmd.Parameters.Add("email", OracleDbType.Varchar2);
            cmd.Parameters.Add("phone", OracleDbType.Varchar2);
            cmd.Parameters.Add("address", OracleDbType.Varchar2);
            cmd.Parameters.Add("city", OracleDbType.Varchar2);
            cmd.Parameters.Add("state", OracleDbType.Varchar2);
            cmd.Parameters.Add("zip_code", OracleDbType.Varchar2);
            cmd.Parameters.Add("country", OracleDbType.Varchar2);
            cmd.Parameters.Add("customer_type", OracleDbType.Varchar2);
            cmd.Parameters.Add("loyalty_points", OracleDbType.Int32);

            string[] customerTypes = { "REGULAR", "PREMIUM", "VIP" };
            string[] cities = { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix" };
            string[] states = { "NY", "CA", "IL", "TX", "AZ" };

            for (int i = 1; i <= 1000; i++)
            {
                cmd.Parameters["first_name"].Value = $"Customer{i}";
                cmd.Parameters["last_name"].Value = $"Last{i}";
                cmd.Parameters["email"].Value = $"customer{i}@example.com";
                cmd.Parameters["phone"].Value = $"555-{random.Next(1000, 9999)}";
                cmd.Parameters["address"].Value = $"{random.Next(1, 9999)} Main St";
                cmd.Parameters["city"].Value = cities[random.Next(cities.Length)];
                cmd.Parameters["state"].Value = states[random.Next(states.Length)];
                cmd.Parameters["zip_code"].Value = $"{random.Next(10000, 99999)}";
                cmd.Parameters["country"].Value = "USA";
                cmd.Parameters["customer_type"].Value = customerTypes[random.Next(customerTypes.Length)];
                cmd.Parameters["loyalty_points"].Value = random.Next(0, 1000);

                cmd.ExecuteNonQuery();
            }
        }

        // Repopulate products (500 products)
        Console.WriteLine("  Inserting 500 products...");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO PRODUCTS (product_id, product_name, description, category, subcategory, price, cost, is_active, created_at)
                VALUES (product_seq.NEXTVAL, :product_name, :description, :category, :subcategory, :price, :cost, 1, CURRENT_TIMESTAMP)";

            cmd.Parameters.Add("product_name", OracleDbType.Varchar2);
            cmd.Parameters.Add("description", OracleDbType.Varchar2);
            cmd.Parameters.Add("category", OracleDbType.Varchar2);
            cmd.Parameters.Add("subcategory", OracleDbType.Varchar2);
            cmd.Parameters.Add("price", OracleDbType.Decimal);
            cmd.Parameters.Add("cost", OracleDbType.Decimal);

            string[] categories = { "Electronics", "Clothing", "Home & Garden", "Sports", "Books" };
            string[] subcategories = { "SubA", "SubB", "SubC", "SubD" };

            for (int i = 1; i <= 500; i++)
            {
                cmd.Parameters["product_name"].Value = $"Product{i}";
                cmd.Parameters["description"].Value = $"Description for Product{i}";
                cmd.Parameters["category"].Value = categories[random.Next(categories.Length)];
                cmd.Parameters["subcategory"].Value = subcategories[random.Next(subcategories.Length)];
                decimal price = random.Next(10, 500);
                cmd.Parameters["price"].Value = price;
                cmd.Parameters["cost"].Value = price * 0.6m; // 40% profit margin
                cmd.ExecuteNonQuery();
            }
        }

        // Repopulate inventory (500 inventory records)
        Console.WriteLine("  Inserting 500 inventory records...");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO INVENTORY (inventory_id, product_id, quantity_available, quantity_reserved, reorder_level)
                VALUES (inventory_seq.NEXTVAL, :product_id, :quantity_available, 0, :reorder_level)";

            cmd.Parameters.Add("product_id", OracleDbType.Int64);
            cmd.Parameters.Add("quantity_available", OracleDbType.Int32);
            cmd.Parameters.Add("reorder_level", OracleDbType.Int32);

            for (int i = 1; i <= 500; i++)
            {
                cmd.Parameters["product_id"].Value = i;
                cmd.Parameters["quantity_available"].Value = random.Next(100, 1000);
                cmd.Parameters["reorder_level"].Value = random.Next(10, 50);
                cmd.ExecuteNonQuery();
            }
        }

        conn.Close();
    }
}
