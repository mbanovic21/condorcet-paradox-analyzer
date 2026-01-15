using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CondorcetWpf;

public partial class App : Application
{
    private Process? _backendProcess;
    private const string BackendExeName = "main";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        StartBackend();
    }

    private void StartBackend()
    {
        Task.Run(() =>
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("main"))
                {
                    try { p.Kill(); p.WaitForExit(500); } catch { }
                }

                string appFolder = AppDomain.CurrentDomain.BaseDirectory;
                string backendPath = Path.Combine(appFolder, "main.exe");

                if (File.Exists(backendPath))
                {
                    _backendProcess = new Process();
                    _backendProcess.StartInfo.FileName = backendPath;
                    _backendProcess.StartInfo.WorkingDirectory = appFolder;
                    _backendProcess.StartInfo.CreateNoWindow = true;
                    _backendProcess.StartInfo.UseShellExecute = false;

                    _backendProcess.Start();
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show($"Greška pri pokretanju backenda: {ex.Message}"));
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_backendProcess != null && !_backendProcess.HasExited)
            {
                _backendProcess.Kill(true);
                _backendProcess.Dispose();
            }
        }
        catch
        {
        }
        finally
        {
            base.OnExit(e);
        }
    }
}