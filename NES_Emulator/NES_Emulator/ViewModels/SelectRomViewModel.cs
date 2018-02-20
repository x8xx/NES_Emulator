using System;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Commands;
using System.Windows.Input;
using NES_Emulator.NES;
using System.IO;
using Xamarin.Forms;

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

        ImageSource _sprite;
        public ImageSource Sprite
        {
            get { return _sprite; }
            set { SetProperty(ref _sprite, value); }
        }

        public SelectRomViewModel(FileManage.IFileSelect fileSelect)
        {
           this.fileSelect = fileSelect;
            SelectRom = new DelegateCommand(() =>
            {
                Test = fileSelect.GetText();
                TestGenerateSprite sprite = new TestGenerateSprite(TestRomBinary.helloWorld);
                sprite.GetSprite();
                sprite.PrintSprit();
                MemoryStream ms = new MemoryStream();
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
