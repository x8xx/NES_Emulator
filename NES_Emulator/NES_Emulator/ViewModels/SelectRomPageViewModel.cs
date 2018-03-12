using System;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Commands;
using System.Windows.Input;
using NES_Emulator.NES;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Diagnostics;

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
                Nes nes = new Nes();
                Test = fileSelect.GetText();
                fileSelect.GetNesList();
                nes.PowerOn(TestRomBinary.helloWorld);
                Sprite = ImageSource.FromStream(() => nes.gameScreen.ScreenMemoryStream);
                Device.StartTimer(TimeSpan.FromMilliseconds(16), () => 
                {
                    nes.gameScreen.notificationScreenUpdate = false;
                    nes.OperatingCpu();
                    return true;
                });
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
