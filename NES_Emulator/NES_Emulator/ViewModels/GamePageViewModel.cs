using System;
using System.Windows.Input;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Commands;
using NES_Emulator.NES;
using Xamarin.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

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
        public ImageSource GameScreen { get { return _gameScreen; } set { SetProperty(ref _gameScreen, value); } }

        public GamePageViewModel()
        {
			nes = Nes.NesInstance;
			UpButtonCommand = new DelegateCommand(() =>
            {
				nes.InputKey(1, 4);
            });
            
			DownButtonCommand = new DelegateCommand(() =>
            {
                nes.InputKey(1, 5);
            });
            
			LeftButtonCommand = new DelegateCommand(() =>
            {
                nes.InputKey(1, 6);
            });
            
			RightButtonCommand = new DelegateCommand(() =>
            {
                nes.InputKey(1, 7);
            });
        }


        /// <summary>
        /// 電源ON
        /// </summary>
        void PowerOn()
        {
            if (nes.PowerOn(rom))
            {
				GameScreen = nes.ppu.GameScreen;
				Device.StartTimer(TimeSpan.FromMilliseconds(16), () =>
				{
					nes.RunEmulator();
					GameScreen = nes.ppu.GameScreen;
					return true;
				});
            }
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
