using System;
using System.Management;
using System.Threading.Tasks;
using System.IO;

namespace RUSBP.Core;

/// <summary>Observa inserción / extracción de USB y resuelve si contiene los archivos PEM válidos.</summary>
public sealed class UsbWatcher : IDisposable
{
    /* ────── Tipos de estado que notificamos ────── */
    public enum UsbStatus { None, Detected, Verified, Error }

    /// <summary>Se dispara cada vez que cambia el estado del USB.</summary>
    public event Action<UsbStatus>? StateChanged;

    private readonly ManagementEventWatcher _insertWatcher;
    private readonly ManagementEventWatcher _removeWatcher;
    private readonly UsbCryptoService _crypto = new();

    private UsbStatus _lastStatus = UsbStatus.None;

    public UsbWatcher()
    {
        _insertWatcher = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2")); // insert
        _removeWatcher = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 3")); // remove

        EventArrivedEventHandler h = async (_, __) => await CheckAsync();
        _insertWatcher.EventArrived += h;
        _removeWatcher.EventArrived += h;

        _insertWatcher.Start();
        _removeWatcher.Start();

        _ = CheckAsync();   // chequeo inicial
    }

    /* ───────────── NÚCLEO ───────────── */
    private async Task CheckAsync()
    {
        await Task.Delay(600); // espera montaje de letra
        UsbStatus newSt = _crypto.TryLocateUsb()
                        ? UsbStatus.Detected            // encontramos un USB con PEM
                        : UsbStatus.None;

        if (newSt != _lastStatus)
            SetStatus(newSt);
    }

    /// <summary>Llamar cuando la verificación contra el backend se completa.</summary>
    public void SetVerified(bool ok)
        => SetStatus(ok ? UsbStatus.Verified : UsbStatus.Error);

    /* ───────────── Helpers ───────────── */

    private void SetStatus(UsbStatus st)
    {
        _lastStatus = st;
        LogDebug($"UsbWatcher → {st}");
        StateChanged?.Invoke(st);
    }

    private static void LogDebug(string msg)
    {
        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "RUSBP", "logs");
            Directory.CreateDirectory(dir);
            string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(Path.Combine(dir, "debug.txt"),
                               $"{ts} - {msg}{Environment.NewLine}");
        }
        catch { /* best-effort */ }
    }

    public void Dispose()
    {
        _insertWatcher.Stop();
        _removeWatcher.Stop();
        _insertWatcher.Dispose();
        _removeWatcher.Dispose();
    }
}
