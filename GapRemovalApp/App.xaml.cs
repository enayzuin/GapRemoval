using System;
using System.IO;
using System.Windows;
using Serilog;
using GapRemovalApp.Logging;
using GapRemovalApp.Utils;
using MessageBox = System.Windows.MessageBox;
using Xabe.FFmpeg;

public partial class App : System.Windows.Application
{
    public App()
    {
        Logger.Setup(); // Inicializa o Serilog

        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ocorreu um erro inesperado para abrir o sistema", ex.ToString());
            File.WriteAllText("startup_crash.log", ex.ToString());
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("DispatcherUnhandledException", e.Exception);
        MessageBox.Show("Ocorreu um erro inesperado. Um log foi salvo.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogError("UnhandledException", e.ExceptionObject as Exception);
    }

    private void LogError(string origin, Exception? ex)
    {
        // Log tradicional em arquivo .txt (como você já fazia)
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
        using (StreamWriter writer = new StreamWriter(logPath, true))
        {
            writer.WriteLine($"[{DateTime.Now}] [{origin}]");
            writer.WriteLine(ex?.ToString());
            writer.WriteLine(new string('-', 80));
        }

        // Novo log com Serilog
        if (ex != null)
            Log.Error(ex, "Exceção capturada por {Origin}", origin);
        else
            Log.Error("Erro desconhecido capturado por {Origin}", origin);
    }
}
