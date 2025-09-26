using System.Runtime.InteropServices;

// Provides low-level mouse input using WinAPI SendInput.
public static class InputSimulator
{
    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public uint type; public INPUTUNION u; }
    [StructLayout(LayoutKind.Explicit)]
    struct INPUTUNION { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk, wScan;
        public uint dwFlags, time;
        public UIntPtr dwExtraInfo;
    }

    private const uint INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_XDOWN = 0x0080;
    private const uint MOUSEEVENTF_XUP = 0x0100;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    public const uint XBUTTON1 = 0x0001;
    public const uint XBUTTON2 = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetCursorPos(out POINT lpPoint);
    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT { public int X, Y; }

    internal static bool TryGetCursorPos(out POINT point)
    {
        if (GetCursorPos(out var p))
        {
            point = new POINT { X = p.X, Y = p.Y };
            return true;
        }

        point = new POINT { X = 0, Y = 0 };
        return false;
    }
    public static bool TrySetCursorPos(int x, int y) => SetCursorPos(x, y);

    // Internal helper to send a mouse event
    static void SendMouseEvent(uint flags, uint mouseData = 0)
    {
        var mi = new MOUSEINPUT
        {
            dx = 0,
            dy = 0,
            mouseData = mouseData,
            dwFlags = flags,
            time = 0,
            dwExtraInfo = UIntPtr.Zero
        };
        var input = new INPUT { type = INPUT_MOUSE, u = new INPUTUNION { mi = mi } };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    public static void LeftDown() => SendMouseEvent(MOUSEEVENTF_LEFTDOWN);
    public static void LeftUp() => SendMouseEvent(MOUSEEVENTF_LEFTUP);
    public static void LeftClick(int downMs = 30)
    {
        LeftDown();
        System.Threading.Thread.Sleep(downMs);
        LeftUp();
    }
    public static void RightDown() => SendMouseEvent(MOUSEEVENTF_RIGHTDOWN);
    public static void RightUp() => SendMouseEvent(MOUSEEVENTF_RIGHTUP);
    public static void XButtonDown(uint which) => SendMouseEvent(MOUSEEVENTF_XDOWN, which);
    public static void XButtonUp(uint which) => SendMouseEvent(MOUSEEVENTF_XUP, which);
}
