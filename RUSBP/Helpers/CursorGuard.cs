using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RUSBP.Helpers;

public static class CursorGuard
{
    private static bool _locked;

    /// <summary>Restringe el cursor a un rectángulo interior del control (padding píxeles).</summary>
    public static void RestrictToControl(Control ctl, Padding pad)
    {
        if (_locked) return;

        var r = ctl.RectangleToScreen(ctl.ClientRectangle);
        r.Inflate(-pad.Left, -pad.Top);

        RECT clip = new() { Left = r.Left, Top = r.Top, Right = r.Right, Bottom = r.Bottom };
        ClipCursor(ref clip);
        _locked = true;
    }

    public static void Release()
    {
        if (!_locked) return;
        RECT empty = new();
        ClipCursor(ref empty);
        _locked = false;
    }

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(ref RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
}
