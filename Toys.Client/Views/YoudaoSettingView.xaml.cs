using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Toys.Client.Views
{
    /// <summary>
    /// YoudaoSettingView.xaml 的交互逻辑
    /// </summary>
    public partial class YoudaoSettingView : Window
    {
        public YoudaoSettingView(object dataContext)
        {
            InitializeComponent();
            this.DataContext = dataContext;
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn.Command.Execute(btn.CommandParameter);
            Close();
        }
    }
}
