using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Esperanta_Klavaro
{
    class KeyboardHook
    {
        #region Windows API Types And Consts

        private const int WH_KEYBOARD_LL = 13;

        private const uint MAPVK_VK_TO_CHAR = 0x02;

        private const uint WM_KEYDOWN = 0x100;

        private const uint VK_SHIFT = 0x10;

        private const uint VK_CAPITAL = 0x14;

        private const uint VK_LCONTROL = 0xA2;

        private const uint VK_SPACE = 0x20;

        private const int KEY_PRESSED = 0x1000;

        private const int KEY_TOGGLED = 0x8000;

        public const int WM_HOTKEY = 0x312;

        delegate int HookProc(int code, IntPtr wParam, IntPtr lParam);
        
        [Flags]
        public enum KBDLLHOOKSTRUCTFlags : uint
        {
            LLKHF_EXTENDED = 0x01,
            LLKHF_INJECTED = 0x10,
            LLKHF_ALTDOWN = 0x20,
            LLKHF_UP = 0x80,
        }

        [Flags]
        public enum Modifiers : uint
        {
            MOD_ALT = 0x1,
            MOD_CONTROL = 0x2,
            MOD_SHIFT = 0x4,
            MOD_WIN = 0x8
        }

        [StructLayout(LayoutKind.Sequential)]
        public class KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public KBDLLHOOKSTRUCTFlags flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }


        #endregion

        #region Windows API Functions

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

        [DllImport("user32.dll")]
        static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPArray)] char[] pwszBuff, int cchBuff, uint wFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        static extern short GetKeyState(uint nVirtKey);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        #endregion

        public event KeyPressEventHandler KeyPressed;
        public event EventHandler HotKeyPressed;

        private bool _hookOn = false;
        public bool hookOn { get { return _hookOn; } }

        private HookProc callbackDelegate;
        private WindowsMessageListener wmListener;
        
        public KeyboardHook()
        {
            Init();
        }

        public KeyboardHook(Modifiers modifiers, Keys key)
        {
            Init();

            RegisterToggleHotKey(modifiers, key);
        }

        private void Init()
        {
            callbackDelegate = new HookProc(KeyboardEventHandler);

            IntPtr modlib = LoadLibrary("user32.dll"); // get a module to bypass SetWindowsHookEx's check on Windows versions earlier than 7

            SetWindowsHookEx(WH_KEYBOARD_LL, callbackDelegate, modlib, 0);

            _hookOn = true;

            wmListener = new WindowsMessageListener(this);
        }

        public bool ToggleHook()
        {
            _hookOn = !_hookOn;
            return _hookOn;
        }

        private int KeyboardEventHandler(int code, IntPtr wParam, IntPtr lParam)
        {
            bool handled = false;
                        
            if (code >= 0 && (uint)wParam == WM_KEYDOWN && KeyPressed != null && _hookOn)
            {
                KBDLLHOOKSTRUCT kbdstruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                byte[] state = new byte[256];
                char[] outchar = new char[1];

                // calling GetKeyState before GetKeyboardState ensures an updated state buffer
                bool ShiftPressed = Convert.ToBoolean(GetKeyState(VK_SHIFT) & KEY_PRESSED);
                bool CapsLockToggled = Convert.ToBoolean(GetKeyState(VK_CAPITAL) & KEY_TOGGLED);

                GetKeyboardState(state);

                // avoid calling ToUnicode on dead keys
                if (MapVirtualKey((uint)kbdstruct.vkCode, MAPVK_VK_TO_CHAR) >> sizeof(uint) * 8 - 1 == 0)
                {
                    if (ToUnicode(kbdstruct.vkCode, kbdstruct.scanCode, state, outchar, outchar.Length, (uint)kbdstruct.flags) == 1)
                    {
                        outchar[0] = ShiftPressed || CapsLockToggled ? Char.ToUpper(outchar[0]) : outchar[0];
                        KeyPressEventArgs e = new KeyPressEventArgs(outchar[0]);
                        KeyPressed(this, e);
                        handled = e.Handled;
                    }
                }
            }
            return handled ? 1 : CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        public void RegisterToggleHotKey(Modifiers modifiers, Keys key)
        {
            RegisterHotKey(wmListener.Handle, wmListener.GetHashCode(), (uint)modifiers, (uint)key);
        }

        public void UnregisterToggleHotKey()
        {
            UnregisterHotKey(wmListener.Handle, wmListener.GetHashCode());
        }

        private void ThrowHotKeyEvent()
        {
            HotKeyPressed?.Invoke(this, null);
        }

        // Nested class to prevent having to make the parent class extend Form
        private partial class WindowsMessageListener : Form
        {
            KeyboardHook kbHook;
            public WindowsMessageListener(KeyboardHook kbHook)
            {
                this.kbHook = kbHook;
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (m.Msg == KeyboardHook.WM_HOTKEY)
                    kbHook.ThrowHotKeyEvent();
            }
        }
    }
}


