using System;
using System.Collections.ObjectModel;

namespace NES_Emulator.FileManage
{
    public interface IFileSelect
    {
        string GetText();
        ObservableCollection<RomFile> GetNesList();
        byte[] GetNesRom(string path);
    }

    public class RomFile
    {
        public string Title { get; set; }

        public RomFile(string title)
        {
            Title = title;
        }
    }
}
