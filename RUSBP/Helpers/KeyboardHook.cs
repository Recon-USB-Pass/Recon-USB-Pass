using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RUSBP.Helpers;

public static class KeyboardHook
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private static IntPtr _hookID = IntPtr.Zero;
    private static readonly LowLevelKeyboardProc _proc = HookCallback;

    public static void Install()
    {
        if (_hookID != IntPtr.Zero) return;
        _hookID = SetHook(_proc);
    }

    public static void Uninstall()
    {
        if (_hookID == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookID);
        _hookID = IntPtr.Zero;
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var cur = System.Diagnostics.Process.GetCurrentProcess();
        using var mod = cur.MainModule!;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
               GetModuleHandle(mod.ModuleName), 0);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 &&
            (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            int vk = Marshal.ReadInt32(lParam);

            // bloquea LWin / RWin y Alt+F4
            if (vk is (int)Keys.LWin or (int)Keys.RWin ||
                (vk == (int)Keys.F4 && (Control.ModifierKeys & Keys.Alt) != 0))
                return (IntPtr)1;          // suprime la tecla
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    /* P/Invoke */
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}

