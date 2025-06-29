using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace GapRemovalApp.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logFilePath;
        private readonly object _lock = new();

        public FileLoggerProvider(string path)
        {
            _logFilePath = path;
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_logFilePath, _lock);
        }

        public void Dispose() { }
    }

    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private readonly object _lock;

        public FileLogger(string path, object lockObj)
        {
            _filePath = path;
            _lock = lockObj;
        }

        public IDisposable? BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_lock)
            {
                File.AppendAllText(_filePath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {formatter(state, exception)}{Environment.NewLine}");
            }
        }
    }
}
