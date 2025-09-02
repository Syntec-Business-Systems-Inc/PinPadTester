using Serilog;
using Serilog.Core;

public class State
{
    public int TriesSinceStateChanged { get; private set; }
    public bool IsSuccess { get; set; }
    public DateTimeOffset StateChangeTime { get; private set; } = DateTimeOffset.Now;

    private readonly Logger _logger;
    public State()
    {
        var logConfig = new LoggerConfiguration().WriteTo.File(path: "C:/Programming/Development/PinPadTester/Logs/StateLogs.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
        _logger = logConfig.CreateLogger();
        IsSuccess = true;
    }

    public void SetState(bool isSuccess, Exception? e)
    {
        if (isSuccess != IsSuccess)
        {
            IsSuccess = isSuccess;
            var previousTime = StateChangeTime;
            var tries = TriesSinceStateChanged;
            StateChangeTime = DateTimeOffset.Now;
            TriesSinceStateChanged = 0;
            var newState = isSuccess ? "PASS" : "FAIL";
            var elapsed = StateChangeTime - previousTime;
            if (e != null)
            {
                _logger.Error(e, $"[{StateChangeTime.ToString("O")}] State changed to {newState}. Tries since state changed = {tries}. Elapsed = {elapsed.TotalMinutes.ToString("N2")} minutes.");
            }
            else
            {
                _logger.Information($"[{StateChangeTime.ToString("O")}] State changed to {newState}. Tries since state changed = {tries}. Elapsed = {elapsed.TotalMinutes.ToString("N2")} minutes.");
            }
        }
        else
        {
            TriesSinceStateChanged += 1;
        }
    }
}