﻿using System;
using Serilog;
using System.IO;
using System.Windows;

namespace GapRemovalApp.Utils;

public static class Logger
{
    public static void Info(string message)
    {
        Log.Information(message);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[INFO] {message}");
        Console.ResetColor();
    }

    public static void Warn(string message)
    {
        Log.Warning(message);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[WARN] {message}");
        Console.ResetColor();
    }

    public static void Setup()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GapRemoval", "logs"
        );
        Directory.CreateDirectory(logDir);

        var logFilePath = Path.Combine(logDir, "app.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public static void Error(string message)
    {
        Log.Error(message);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }

    public static void Debug(string message)
    {
        Log.Debug(message);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[DEBUG] {message}");
        Console.ResetColor();
    }
}
