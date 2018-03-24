using System;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Commands;
using System.Windows.Input;
using NES_Emulator.NES;
using System.Collections.ObjectModel;
using NES_Emulator.FileManage;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Diagnostics;

namespace NES_Emulator.ViewModels
{
    public class SelectRomPageViewModel: BindableBase, INavigationAware
    {
        public ICommand SelectRom { get; }
        public Command<RomFile> LoadRom { get; }

        ObservableCollection<RomFile> _nesList;
        public ObservableCollection<RomFile> NesList
        {
            get { return _nesList; }
            set { SetProperty(ref _nesList, value); }
        }

        public string _romTitle;
        public string RomTitle
        {
            get { return _romTitle; }
            set { SetProperty(ref _romTitle, value); }
        }

        public SelectRomPageViewModel(INavigationService navigationService, IFileSelect fileSelect)
        {
            NesList = fileSelect.GetNesList();
            Debug.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            LoadRom = new Command<RomFile>(x => 
            {
                var navigationParameters = new NavigationParameters();
                navigationParameters.Add("rom", fileSelect.GetNesRom(x.Title));
                navigationService.NavigateAsync("GamePage", navigationParameters);
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
