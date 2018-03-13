using System;
using System.Collections.ObjectModel;
using NES_Emulator.FileManage;
using UIKit;
using CloudKit;
using Foundation;
using System.Diagnostics;

namespace NES_Emulator.iOS.FileManage
{
    public class FileSelect : IFileSelect
    {
        public string GetText()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        }

        /// <summary>
        ///  アプリケーションフォルダのRom一覧を取得
        /// </summary>
        /// <returns>The nes list.</returns>
        public ObservableCollection<RomFile> GetNesList()
        {
            NSFileManager file = new NSFileManager();
            var nesList = new ObservableCollection<RomFile>();
            foreach(var name in file.Subpaths(Environment.GetFolderPath(Environment.SpecialFolder.Personal)))
            {
                if(name.Contains(".nes")) nesList.Add(new RomFile(name));
            }
            return nesList;
        }

        public byte[] GetNesRom(string romName)
        {
            NSData data = NSData.FromFile(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/" + romName);
            return data.ToArray();
        }
    }
}
