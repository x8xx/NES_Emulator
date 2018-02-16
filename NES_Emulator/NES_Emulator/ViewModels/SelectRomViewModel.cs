using System;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Commands;
using System.Windows.Input;

namespace NES_Emulator.ViewModels
{
    public class SelectRomViewModel: BindableBase, INavigationAware
    {
        public ICommand SelectRom { get; }
        FileManage.IFileSelect fileSelect;

        string _test;
        public string Test
        {
            get { return _test; }
            set { SetProperty(ref _test, value); }
        }

        public SelectRomViewModel(FileManage.IFileSelect fileSelect)
        {
           this.fileSelect = fileSelect;
            SelectRom = new DelegateCommand(() =>
            {
                Test = fileSelect.GetText();
                //Test = "aiueoaiueo";
            });
        }

        public void OnNavigatedFrom(NavigationParameters parameters)
        {

        }

        public void OnNavigatedTo(NavigationParameters parameters)
        {

        }

        public void OnNavigatingTo(NavigationParameters parameters)
        {

        }
    }
}
