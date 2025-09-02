// See https://aka.ms/new-console-template for more information
using PinPadTester;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text.Json;

Console.WriteLine("Hello, World!");

// todo:



static void StartUp()
{
    var logConfig = new LoggerConfiguration().WriteTo.File(new CompactJsonFormatter(), path: "C:/Programming/Development/PinPadTester/Logs/log.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
    Log.Logger = logConfig.CreateLogger();

    var test = new GlobalTest("", 8080);

    int successesSinceLastFail = 0;
    var results = new List<FailInfo>();
    var state = new State();
    Log.Information("Starting");
    while (true)
    {
        try
        {
            test.Go().Wait();
            state.SetState(true, null);
            successesSinceLastFail += 1;
            Thread.Sleep(TimeSpan.FromSeconds(Random.Shared.Next(30, 90)));

        }
        catch (Exception ex)
        {
            state.SetState(false, ex);
            Log.Error(ex, "Pin Pad Test failed");
            results.Add(new()
            {
                SuccessesBefore = successesSinceLastFail,
                FailTime = DateTime.Now,
                Message = ex.Message
            });
            successesSinceLastFail = 0;
            Thread.Sleep(TimeSpan.FromSeconds(Random.Shared.Next(30, 90)));
            File.WriteAllText("C:/Programming/Development/PinPadTester/Results1.json", JsonSerializer.Serialize(results));
        }
    }

}

StartUp();