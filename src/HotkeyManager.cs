using System;
using System.Runtime.InteropServices;

namespace EcoNotepad
{
    /// <summary>Registers the global hotkeys used by the overlay window.</summary>
    public sealed class HotkeyManager : IDisposable
    {
        public const int IdToggleVisible = 1;
        public const int IdToggleClickThrough = 2;

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_NOREPEAT = 0x4000;

        // Ctrl+Shift+N and Ctrl+Shift+T (chosen to avoid AltGr conflicts on
        // Spanish/European keyboard layouts)
        private const uint VK_N = 0x4E;
        private const uint VK_T = 0x54;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly IntPtr _hwnd;
        private bool _registered;

        public HotkeyManager(IntPtr hwnd)
        {
            _hwnd = hwnd;
        }

        public bool Register()
        {
            _registered = RegisterHotKey(_hwnd, IdToggleVisible, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_N)
                       && RegisterHotKey(_hwnd, IdToggleClickThrough, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_T);
            return _registered;
        }

        public void Unregister()
        {
            if (_registered)
            {
                UnregisterHotKey(_hwnd, IdToggleVisible);
                UnregisterHotKey(_hwnd, IdToggleClickThrough);
                _registered = false;
            }
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}
