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
        using (var reader = cmd.ExecuteReaderAsync().Result)
        {
            while (reader.ReadAsync().Result) Console.WriteLine(reader.GetString(0));
        }
    }

    [TestMethod]
    public void TestThread()
    {
        var autoEvent = new AutoResetEvent(false);

        var workerThread = new Thread(() =>
        {
            for (var i = 0; i < 5; i++)
            {
                Console.WriteLine("Worker thread waiting for event to be set.");
                autoEvent.WaitOne();
                Console.WriteLine("Event was set, continuing execution.");

                // 模拟一些工作
                Thread.Sleep(1000);
            }
        });

        workerThread.Start();

        for (var i = 0; i < 5; i++)
        {
            // 模拟主线程或其他线程在某个条件下置位事件
            Console.WriteLine($"Main thread setting event (i={i}).");
            autoEvent.Set();
            Thread.Sleep(500); // 模拟其他工作
        }

        workerThread.Join();
        Console.WriteLine("Main thread completed.");
    }
}