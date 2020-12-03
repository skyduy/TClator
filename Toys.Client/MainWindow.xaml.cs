using System;
using System.Windows;
using System.Windows.Input;
using Toys.Client.ViewModels;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls;

namespace Toys.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<HotKey> Hotkeys { get; set; } = new List<HotKey>();
        public MainWindow()
        {
            InitializeComponent();

            Left = (SystemParameters.WorkArea.Width - Width) / 2;
            Top = SystemParameters.WorkArea.Height / 5;

            DataContext = new MainWindowViewModel();
            PreviewKeyDown += new KeyEventHandler(HandleHotkey);
            InputBox.Focus();

            HotKey AltQ = new HotKey(Key.Q, KeyModifier.Alt, OnHotKeyHandler);
            if (AltQ.Register())
            {
                Hotkeys.Add(AltQ);
            }
            else
            {
                MessageBox.Show("快捷键 Alt + Q 已被其它应用注册！");
            }
        }

        private void OnHotKeyHandler(HotKey hotKey)
        {
            Show();
            Activate();
            InputBox.SelectAll();
            InputBox.Focus();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Hide();
        }

        private void HandleHotkey(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
            {
                // 处理系统 Hot Key 残留下来的 Modifier，忽略所有 Alt 开头的命令
                e.Handled = true;
                return;
            }

            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (InputBox.IsFocused)
                    {
                        var first = ResultList.ItemContainerGenerator.ContainerFromIndex(0);
                        if (first != null)
                        {
                            ResultList.Focus();
                            ((ListBoxItem)first).Focus();
                            e.Handled = true;
                        }
                    }
                    break;
                case Key.Up:
                    if (ResultList.SelectedIndex == 0)
                    {
                        var first = (ListBoxItem)ResultList.ItemContainerGenerator.ContainerFromIndex(0);
                        if (first.IsFocused)
                        {
                            InputBox.Focus();
                            e.Handled = true;
                        }
                    }
                    break;
            }
        }

        private void Toys_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
            Activate();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (MainWindowViewModel)DataContext;
            if (viewModel.ChangeSettingCommand.CanExecute())
            {
                viewModel.ChangeSettingCommand.Execute();
            }
        }

        private void Exist_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (MainWindowViewModel)DataContext;
            if (viewModel.ExitCommand.CanExecute())
            {
                viewModel.ExitCommand.Execute();
            }

            foreach (var item in Hotkeys)
            {
                item.Unregister();
            }
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }

    // ******************************************************************
    public class HotKey : IDisposable
    {
        private static Dictionary<int, HotKey> _dictHotKeyToCalBackProc;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, UInt32 fsModifiers, UInt32 vlc);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int WmHotKey = 0x0312;

        private bool _disposed = false;

        public Key Key { get; private set; }
        public KeyModifier KeyModifiers { get; private set; }
        public Action<HotKey> Action { get; private set; }
        public int Id { get; set; }

        public HotKey(Key k, KeyModifier keyModifiers, Action<HotKey> action)
        {
            Key = k;
            KeyModifiers = keyModifiers;
            Action = action;
        }

        public bool Register()
        {
            int virtualKeyCode = KeyInterop.VirtualKeyFromKey(Key);
            Id = virtualKeyCode + ((int)KeyModifiers * 0x10000);
            bool result = RegisterHotKey(IntPtr.Zero, Id, (UInt32)KeyModifiers, (UInt32)virtualKeyCode);

            if (_dictHotKeyToCalBackProc == null)
            {
                _dictHotKeyToCalBackProc = new Dictionary<int, HotKey>();
                ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(ComponentDispatcherThreadFilterMessage);
            }

            _dictHotKeyToCalBackProc.Add(Id, this);

            Debug.Print(result.ToString() + ", " + Id + ", " + virtualKeyCode);
            return result;
        }

        public void Unregister()
        {
            HotKey hotKey;
            if (_dictHotKeyToCalBackProc.TryGetValue(Id, out hotKey))
            {
                UnregisterHotKey(IntPtr.Zero, Id);
            }
        }

        private static void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            if (!handled)
            {
                if (msg.message == WmHotKey)
                {
                    HotKey hotKey;

                    if (_dictHotKeyToCalBackProc.TryGetValue((int)msg.wParam, out hotKey))
                    {
                        if (hotKey.Action != null)
                        {
                            hotKey.Action.Invoke(hotKey);
                        }
                        handled = true;
                    }
                }
            }
        }

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be _disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be _disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    Unregister();
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }
    }

    [Flags]
    public enum KeyModifier
    {
        None = 0x0000,
        Alt = 0x0001,
        Ctrl = 0x0002,
        NoRepeat = 0x4000,
        Shift = 0x0004,
        Win = 0x0008
    }
}
