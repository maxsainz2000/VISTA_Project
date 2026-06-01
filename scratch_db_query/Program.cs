using System;
using System.Data;
using MySql.Data.MySqlClient;

class Program
{
    static void Main()
    {
        string connStr = "Server=localhost;Port=3306;Database=merchsys_central;User Id=root;Password=;";
        using (MySqlConnection conn = new MySqlConnection(connStr))
        {
            try
            {
                conn.Open();
                Console.WriteLine("Connection successful!");
                
                string[] queries = {
                    "SELECT QuantityRemaining FROM Inv_StockBatches WHERE ProductId = 11;",
                    "SELECT Quantity, MovementType FROM Inv_StockMovements WHERE ProductId = 11 ORDER BY Id DESC LIMIT 1;",
                    "SELECT Category, Amount FROM Acc_ExpenseRecords WHERE Category = 'Shrinkage' ORDER BY Id DESC LIMIT 1;"
                };

                foreach (var sql in queries) {
                    Console.WriteLine($"\nQuery: {sql}");
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        for (int i = 0; i < rdr.FieldCount; i++) Console.Write(rdr.GetName(i) + "\t");
                        Console.WriteLine();
                        while (rdr.Read())
                        {
                            for (int i = 0; i < rdr.FieldCount; i++) Console.Write(rdr[i]?.ToString() + "\t");
                            Console.WriteLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
