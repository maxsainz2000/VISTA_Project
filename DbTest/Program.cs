using System;
using MySqlConnector;

var connectionString = "Server=localhost;Port=3306;Database=merchsys_central;User Id=root;Password=;";
using var connection = new MySqlConnection(connectionString);
try
{
    connection.Open();
    Console.WriteLine("Connected to database successfully.");

    var sql = "SELECT TransactionNumber, TransactionDate, TotalAmount FROM pos_salestransactions LIMIT 10;";
    using var command = new MySqlCommand(sql, connection);
    using var reader = command.ExecuteReader();

    Console.WriteLine("TxNumber | Date | TotalAmount");
    Console.WriteLine("-------------------------");
    while (reader.Read())
    {
        var tx = reader.GetString(0);
        var dt = reader.GetDateTime(1).ToString("yyyy-MM-dd HH:mm:ss");
        var tot = reader.GetDecimal(2).ToString();
        Console.WriteLine($"{tx} | {dt} | {tot}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
























