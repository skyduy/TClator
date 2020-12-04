using System.Windows;
using System.Windows.Input;
using Toys.Client.ViewModels;

namespace Toys.Client.Views
{
    /// <summary>
    /// ResultDetailView.xaml 的交互逻辑
    /// </summary>
    public partial class TranslateResultDetailView : Window
    {
        public TranslateResultDetailView()
        {
            InitializeComponent();
            Left = (SystemParameters.WorkArea.Width - Width) / 2;
            Top = SystemParameters.WorkArea.Height / 6;

            PreviewKeyDown += new KeyEventHandler(HandleEsc);
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Hide();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 禁用 Alt
            if (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt)
            {
                e.Handled = true;
                return;
            }
        }
    }
}
