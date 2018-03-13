using System;
using System.Windows.Input;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Commands;
using NES_Emulator.NES;
using NES_Emulator.FileManage;
using Xamarin.Forms;
using System.Diagnostics;

namespace NES_Emulator.ViewModels
{
    public class GamePageViewModel : BindableBase, INavigationAware
    {
        byte[] rom;

        ImageSource _gameScreen;
        public ImageSource GameScreen
        {
            get { return _gameScreen; }
            set { SetProperty(ref _gameScreen, value); }
        }

        public GamePageViewModel()
        {
            
        }

        void PowerOn()
        {
            Nes nes = new Nes();
            nes.PowerOn(rom);
            GameScreen = ImageSource.FromStream(() => nes.gameScreen.ScreenMemoryStream);
            Device.StartTimer(TimeSpan.FromMilliseconds(16), () =>
            {
                nes.gameScreen.notificationScreenUpdate = false;
                nes.OperatingCpu();
                return true;
            });
        }

        public void OnNavigatedFrom(NavigationParameters parameters)
        {
            
        }

        public void OnNavigatedTo(NavigationParameters parameters)
        {
            rom = (byte[])parameters["rom"];
            PowerOn();
        }

        public void OnNavigatingTo(NavigationParameters parameters)
        {

        }
    }
}
