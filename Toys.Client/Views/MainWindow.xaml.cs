using System;
using System.Windows;
using System.Windows.Input;
using Toys.Client.ViewModels;
using System.Windows.Controls;

namespace Toys.Client.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly TranslateResultDetailView detailView;
        public MainWindow()
        {
            InitializeComponent();

            Left = (SystemParameters.WorkArea.Width - Width) / 2;
            Top = SystemParameters.WorkArea.Height / 5;

            MainWindowViewModel viewModel = new MainWindowViewModel();
            detailView = new TranslateResultDetailView()
            {
                DataContext = viewModel.DetailViewModel
            };
            viewModel.ShowDetailAction = new Action(ShowTranslateDetailDialog);
            viewModel.ActivateMainWindowAction = new Action(ActivateMainWindow);
            DataContext = viewModel;

            InputBox.Focus();
            Hide();
        }

        void ShowTranslateDetailDialog()
        {
            detailView.Show();
            detailView.Focus();
        }

        private void ActivateMainWindow()
        {
            Activate();
            InputBox.SelectAll();
            Show();
            InputBox.Focus();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            detailView.Hide();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Hide();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 禁用 Alt
            if (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt)
            {
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

            detailView.Close();
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            using (tb.DeclareChangeBlock())
            {
                foreach (var c in e.Changes)
                {
                    if (c.AddedLength == 0) continue;
                    tb.Select(c.Offset, c.AddedLength);
                    if (tb.SelectedText.Contains(Environment.NewLine))
                    {
                        tb.SelectedText = tb.SelectedText.Replace(Environment.NewLine, " ");
                    }
                    if (tb.SelectedText.Contains('\n'))
                    {
                        tb.SelectedText = tb.SelectedText.Replace('\n', ' ');
                    }
                    tb.Select(c.Offset + c.AddedLength, 0);
                }
            }
        }
    }
}
