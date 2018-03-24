using System;
using System.Collections.ObjectModel;
using NES_Emulator.FileManage;

namespace NES_Emulator.Droid.FileManage
{
    public class FileSelect : IFileSelect
    {
        public void GetText()
        {
            
        }

        public ObservableCollection<RomFile> GetNesList()
        {
            return null;
        }

        public byte[] GetNesRom(string romName)
        {
            return null;
        }

    }
}
