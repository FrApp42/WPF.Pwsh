using EasyWindowsTerminalControl;
using EasyWindowsTerminalControl.Internals;
using Microsoft.Terminal.Wpf;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FrApp42.WPF.Pwsh
{
    /// <summary>
    /// A real PowerShell (ConPTY) session hosted in a WPF control.
    /// Set <see cref="UserControl.FontFamily"/> / <see cref="UserControl.FontSize"/> to change the terminal font.
    /// </summary>
    public class PwshTerminal : UserControl, IDisposable
    {
        private readonly EasyTerminalControl eTerminal = new();
        private readonly DispatcherTimer _exitWatch = new() { Interval = TimeSpan.FromMilliseconds(500) };
        private bool _disposed;

        /// <summary>Raised when the shell process exits.</summary>
        public event EventHandler<int>? OnSessionDisconnected;

        public PwshTerminal() : this(DefaultShell()) { }

        public PwshTerminal(string startupCommandLine)
        {
            eTerminal.StartupCommandLine = startupCommandLine;
            Content = eTerminal;
            FontFamily = new FontFamily("MesloLGS Nerd Font Mono");
            FontSize = 14;
            Loaded += OnFirstLoad;
        }

        /// <summary>Working directory the shell starts in.</summary>
        public string WorkingDirectory
        {
            get => eTerminal.WorkingDirectory;
            set => eTerminal.WorkingDirectory = value;
        }

        /// <summary>PowerShell 7 when it is on the PATH, Windows PowerShell otherwise.</summary>
        private static string DefaultShell()
            => (Environment.GetEnvironmentVariable("Path") ?? "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Any(p => File.Exists(Path.Combine(p.Trim(), "pwsh.exe")))
                ? "pwsh.exe" : "powershell.exe";

        /// <summary>Campbell palette (the Windows Terminal default) and the control's font.</summary>
        private void ApplyTheme()
        {
            uint Bgr(uint rgb) => (rgb & 0xFF) << 16 | (rgb & 0xFF00) | (rgb >> 16);

            eTerminal.FontFamilyWhenSettingTheme = FontFamily;
            eTerminal.FontSizeWhenSettingTheme = (int)FontSize;
            eTerminal.Theme = new TerminalTheme
            {
                DefaultBackground = Bgr(0x0C0C0C),
                DefaultForeground = Bgr(0xCCCCCC),
                DefaultSelectionBackground = Bgr(0x3A96DD),
                CursorStyle = CursorStyle.BlinkingBar,
                ColorTable = new uint[]
                {
                    0x0C0C0C, 0xC50F1F, 0x13A10E, 0xC19C00, 0x0037DA, 0x881798, 0x3A96DD, 0xCCCCCC,
                    0x767676, 0xE74856, 0x16C60C, 0xF9F1A5, 0x3B78FF, 0xB4009E, 0x61D6D6, 0xF2F2F2
                }.Select(Bgr).ToArray()
            };
        }

        // ponytail: process polling, EasyTerminalControl exposes no exit event.
        private void OnFirstLoad(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnFirstLoad;
            ApplyTheme();

            // ConPTYTerm lives on the UI thread, so the watch runs there too.
            _exitWatch.Tick += (_, _) =>
            {
                if (!HasExited(eTerminal.ConPTYTerm?.Process)) return;

                _exitWatch.Stop();
                OnSessionDisconnected?.Invoke(this, 0);
            };
            _exitWatch.Start();
        }

        // TermPTY disposes the process right after it exits, so a throwing HasExited means the shell is gone.
        private static bool HasExited(IProcess? process)
        {
            if (process == null) return false;
            try { return process.HasExited; } catch { return true; }
        }

        /// <summary>Kills the shell process.</summary>
        public void Disconnect()
        {
            try { eTerminal.ConPTYTerm?.Process?.Kill(true); } catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _exitWatch.Stop();
            Disconnect();
            try { eTerminal.DisconnectConPTYTerm()?.StopExternalTermOnly(); } catch { }
        }
    }
}
