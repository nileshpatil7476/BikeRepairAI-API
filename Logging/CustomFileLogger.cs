
using System.Collections.Concurrent;

namespace BikeRepairAI.Logging;

public class CustomFileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly ConcurrentDictionary<string, CustomFileLogger> _loggers = new();

    public CustomFileLoggerProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(
            categoryName,
            name => new CustomFileLogger(name, _logDirectory));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

public class CustomFileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logDirectory;
    private static readonly object FileLock = new();

    public CustomFileLogger(string categoryName, string logDirectory)
    {
        _categoryName = categoryName;
        _logDirectory = logDirectory;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);

        var logMessage =
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} " +
            $"[{logLevel}] [{_categoryName}] {message}";

        if (exception != null)
            logMessage += Environment.NewLine + exception;

        var filePath = Path.Combine(
            _logDirectory,
            $"BikeRepairAI-{DateTime.Now:yyyy-MM-dd}.log");

        lock (FileLock)
        {
            File.AppendAllText(
                filePath,
                logMessage + Environment.NewLine);
        }
    }
}
