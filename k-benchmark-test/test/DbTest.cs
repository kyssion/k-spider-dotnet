using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

[TestClass]
public class DbTest
{
    [TestMethod]
    public void Test()
    {
        var connectionString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";
         using var dataSource = NpgsqlDataSource.Create(connectionString);

         using var item = new NpgsqlConnection();
         item.Open();
// Insert some data
         using (var cmd = dataSource.CreateCommand("INSERT INTO data (some_field) VALUES ($1)"))
        {
            cmd.Parameters.AddWithValue("Hello world");
             cmd.ExecuteNonQueryAsync().Wait();
        }

// Retrieve all rows
         using (var cmd = dataSource.CreateCommand("SELECT some_field FROM data"))
         using (var reader =  cmd.ExecuteReaderAsync().Result)
        {
            while ( reader.ReadAsync().Result)
            {
                Console.WriteLine(reader.GetString(0));
            }
        }
    }
}