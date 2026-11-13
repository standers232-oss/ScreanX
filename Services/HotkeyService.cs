using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScreanX.Services
{
    public class HotkeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        public const uint VK_SNAPSHOT = 0x2C; // PrintScreen
        public const uint VK_S = 0x53;        // S key
        public const uint VK_R = 0x52;        // R key (Record)
        public const uint VK_C = 0x43;        // C key (Color Picker)
        public const uint VK_M = 0x4D;        // M key (Measure Ruler)

        private IntPtr _hWnd;
        private HwndSource? _source;
        private int _currentId = 9000;
        private readonly Dictionary<int, Action> _callbacks = new();

        public void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _hWnd = helper.EnsureHandle();
            _source = HwndSource.FromHwnd(_hWnd);
            _source?.AddHook(HwndHook);
        }

        public int Register(uint modifiers, uint vk, Action callback)
        {
            if (_hWnd == IntPtr.Zero) return -1;

            int id = ++_currentId;
            bool success = RegisterHotKey(_hWnd, id, modifiers | MOD_NOREPEAT, vk);
            if (success)
            {
                _callbacks[id] = callback;
                return id;
            }
            return -1;
        }

        public void Unregister(int id)
        {
            if (_callbacks.ContainsKey(id))
            {
                UnregisterHotKey(_hWnd, id);
                _callbacks.Remove(id);
            }
        }

        public void UnregisterAll()
        {
            foreach (var id in _callbacks.Keys)
            {
                UnregisterHotKey(_hWnd, id);
            }
            _callbacks.Clear();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_callbacks.TryGetValue(id, out var action))
                {
                    action.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterAll();
            _source?.RemoveHook(HwndHook);
            _source?.Dispose();
            _source = null;
        }
    }
}
