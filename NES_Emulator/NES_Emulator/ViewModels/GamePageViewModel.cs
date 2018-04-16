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
		public ICommand UpButtonCommand { get; }
		public ICommand DownButtonCommand { get; }
		public ICommand LeftButtonCommand { get; }
		public ICommand RightButtonCommand { get; }

        byte[] rom;
		Nes nes;

        ImageSource _gameScreen;
        public ImageSource GameScreen
        {
            get { return _gameScreen; }
            set { SetProperty(ref _gameScreen, value); }
        }

        public GamePageViewModel()
        {
			UpButtonCommand = new DelegateCommand(() =>
            {
				nes.controller.InputKey(1, 4);
            });
            
			DownButtonCommand = new DelegateCommand(() =>
            {
                nes.controller.InputKey(1, 5);
            });
            
			LeftButtonCommand = new DelegateCommand(() =>
            {
                nes.controller.InputKey(1, 6);
            });
            
			RightButtonCommand = new DelegateCommand(() =>
            {
                nes.controller.InputKey(1, 7);
            });
        }

        void PowerOn()
        {
            nes = new Nes();
            nes.PowerOn(rom);

            Device.StartTimer(TimeSpan.FromMilliseconds(16), () =>
            {
                GameScreen = ImageSource.FromStream(() => nes.gameScreen.ScreenMemoryStream);
                nes.ppu.notificationScreenUpdate = false;
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
