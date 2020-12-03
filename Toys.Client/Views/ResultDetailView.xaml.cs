using System.Windows;
using System.Windows.Input;

namespace Toys.Client.Views
{
    /// <summary>
    /// ResultDetailView.xaml 的交互逻辑
    /// </summary>
    public partial class ResultDetailView : Window
    {
        public ResultDetailView(string src, string dst)
        {
            InitializeComponent();

            Left = (SystemParameters.WorkArea.Width - Width) / 2;
            Top = SystemParameters.WorkArea.Height / 6;

            DataContext = new MiniResultDetailViewModel(src, dst);
            PreviewKeyDown += new KeyEventHandler(HandleEsc);

            ResultBox.Focus();
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }

    class MiniResultDetailViewModel
    {
        public string Src { get; set; }
        public string Dst { get; set; }
        public MiniResultDetailViewModel(string src, string dst)
        {
            Src = src;
            Dst = dst;
        }
    }
}
