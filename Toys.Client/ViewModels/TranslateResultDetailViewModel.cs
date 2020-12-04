using Prism.Mvvm;

namespace Toys.Client.ViewModels
{
    class TranslateResultDetailViewModel : BindableBase
    {
        private string src;
        public string Src
        {
            get { return src; }
            set
            {
                src = value;
                RaisePropertyChanged(nameof(Src));
            }
        }

        private string dst;
        public string Dst
        {
            get { return dst; }
            set
            {
                dst = value;
                RaisePropertyChanged(nameof(Dst));
            }
        }
    }
}
