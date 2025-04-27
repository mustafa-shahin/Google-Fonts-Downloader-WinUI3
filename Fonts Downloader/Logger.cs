using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fonts_Downloader
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.json");
        private static readonly object _lockObject = new();

        public static async Task HandleErrorAsync(string userMessage, Exception ex, ContentDialog dialog = null)
        {
            try
            {
                var logMessage = CreateLogMessage(userMessage, ex);
                await AppendLogToFileAsync(logMessage);

                // Don't show dialog for minor issues
                if (!string.IsNullOrEmpty(userMessage) &&
                    !userMessage.Contains("Network check") &&
                    !userMessage.Contains("Download link is empty") &&
                    dialog != null)
                {
                    dialog.Title = "Error";
                    dialog.Content = userMessage;
                    dialog.PrimaryButtonText = "OK";
                    await dialog.ShowAsync();
                }

                Debug.WriteLine($"Error: {userMessage}, Details: {ex?.Message}");
            }
            catch (Exception logEx)
            {
                // Last resort if logging fails
                Debug.WriteLine($"Logging failed: {logEx.Message}");
                try
                {
                    await File.WriteAllTextAsync(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt"),
                        $"[{DateTime.Now}] Error: {userMessage}, Details: {ex?.Message}"
                    );
                }
                catch
                {
                    // Nothing more we can do
                }
            }
        }

        public static void HandleError(string userMessage, Exception ex)
        {
            try
            {
                var logMessage = CreateLogMessage(userMessage, ex);
                AppendLogToFile(logMessage);

                Debug.WriteLine($"Error: {userMessage}, Details: {ex?.Message}");
            }
            catch (Exception logEx)
            {
                // Last resort if logging fails
                Debug.WriteLine($"Logging failed: {logEx.Message}");
                try
                {
                    File.WriteAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt"),
                        $"[{DateTime.Now}] Error: {userMessage}, Details: {ex?.Message}"
                    );
                }
                catch
                {
                    // Nothing more we can do
                }
            }
        }

        private static LogMessage CreateLogMessage(string message, Exception ex)
        {
            return new LogMessage
            {
                Msg = message,
                Data = new ErrorData
                {
                    ExceptionMessage = ex?.Message,
                    ExceptionType = ex?.GetType().FullName,
                    StackTrace = ex?.StackTrace
                },
                EpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Level = "ERROR",
                Id = Guid.NewGuid().ToString()
            };
        }

        private static async Task AppendLogToFileAsync(LogMessage logMessage)
        {
            try
            {
                var logEntries = new List<LogMessage>();

                // Read existing logs
                if (File.Exists(LogFilePath))
                {
                    var existingLogs = await File.ReadAllTextAsync(LogFilePath);
                    if (!string.IsNullOrEmpty(existingLogs))
                    {
                        logEntries = JsonSerializer.Deserialize<List<LogMessage>>(existingLogs) ?? new List<LogMessage>();
                    }
                }

                // Add the new log entry
                logEntries.Add(logMessage);

                // Keep only the most recent 100 logs to prevent the file from growing too large
                if (logEntries.Count > 100)
                {
                    logEntries = logEntries.OrderByDescending(l => l.EpochMs).Take(100).ToList();
                }

                // Write back to the file
                var json = JsonSerializer.Serialize(logEntries, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(LogFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }

        private static void AppendLogToFile(LogMessage logMessage)
        {
            lock (_lockObject)
            {
                try
                {
                    var logEntries = new List<LogMessage>();

                    // Read existing logs
                    if (File.Exists(LogFilePath))
                    {
                        var existingLogs = File.ReadAllText(LogFilePath);
                        if (!string.IsNullOrEmpty(existingLogs))
                        {
                            logEntries = JsonSerializer.Deserialize<List<LogMessage>>(existingLogs) ?? new List<LogMessage>();
                        }
                    }

                    // Add the new log entry
                    logEntries.Add(logMessage);

                    // Keep only the most recent 100 logs to prevent the file from growing too large
                    if (logEntries.Count > 100)
                    {
                        logEntries = logEntries.OrderByDescending(l => l.EpochMs).Take(100).ToList();
                    }

                    // Write back to the file
                    var json = JsonSerializer.Serialize(logEntries, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(LogFilePath, json);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }

        public static async Task<IEnumerable<LogMessage>> GetRecentLogsAsync(int count = 20)
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    return Enumerable.Empty<LogMessage>();
                }

                var logContent = await File.ReadAllTextAsync(LogFilePath);
                var logEntries = JsonSerializer.Deserialize<List<LogMessage>>(logContent) ?? new List<LogMessage>();
                return logEntries.OrderByDescending(l => l.EpochMs).Take(count);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading logs: {ex.Message}");
                return Enumerable.Empty<LogMessage>();
            }
        }

        public static async Task ClearLogsAsync()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    await File.WriteAllTextAsync(LogFilePath, "[]");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing logs: {ex.Message}");
            }
        }
    }

    public class LogMessage
    {
        public string Msg { get; set; }
        public ErrorData Data { get; set; }
        public long EpochMs { get; set; }
        public string Level { get; set; }
        public string Id { get; set; }

        public DateTime Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(EpochMs).LocalDateTime;
    }

    public class ErrorData
    {
        public string ExceptionMessage { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
    }
}