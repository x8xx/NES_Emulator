using System;
using NES_Emulator.FileManage;

namespace NES_Emulator.iOS.FileManage
{
    public class FileSelect : IFileSelect
    {
        public string GetText()
        {
            return "Hello iOS";
        }
    }
}
